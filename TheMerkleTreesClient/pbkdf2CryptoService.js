class PBKDF2CryptoService {
  constructor() {
    this.iterations = 100000;
    this.keyLength = 256;
    this.ivLength = 12; // 12 octets est la taille recommandée pour AES-GCM
    this.saltLength = 16;
    this.userPassword = null;
    this.encryptionKey = null;
    this.passwordExpiration = 30 * 60 * 1000; // 30 minutes en millisecondes
  }

  // Gestion du mot de passe utilisateur
  setUserPassword(password) {
    this.userPassword = password;
    this.passwordSetTime = Date.now();
  }

  clearUserPassword() {
    this.userPassword = null;
    this.passwordSetTime = null;
  }

  hasUserPassword() {
    // Vérifier si le mot de passe existe et n'a pas expiré
    if (this.userPassword !== null && this.userPassword !== undefined) {
      if (this.passwordSetTime && (Date.now() - this.passwordSetTime > this.passwordExpiration)) {
        console.log("Mot de passe expiré, effacement automatique");
        this.clearUserPassword();
        return false;
      }
      return true;
    }
    return false;
  }

  // Génération de sel
  generateSalt() {
    return window.crypto.getRandomValues(new Uint8Array(this.saltLength));
  }

  // Génération d'IV aléatoire
  generateIV() {
    return window.crypto.getRandomValues(new Uint8Array(this.ivLength));
  }

  // Dérivation de clé
  async deriveKey(password, salt) {
    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);

    const baseKey = await window.crypto.subtle.importKey(
      "raw",
      passwordBuffer,
      { name: "PBKDF2" },
      false,
      ["deriveBits", "deriveKey"]
    );

    const key = await window.crypto.subtle.deriveKey(
      {
        name: "PBKDF2",
        salt: salt,
        iterations: this.iterations,
        hash: "SHA-256"
      },
      baseKey,
      {
        name: "AES-GCM",
        length: this.keyLength
      },
      true,
      ["encrypt", "decrypt"]
    );

    return key;
  }

  // Chiffrement de fichier
  async encryptFile(file) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    try {
      const salt = this.generateSalt();
      const key = await this.deriveKey(this.userPassword, salt);
      const iv = this.generateIV();
      const fileBuffer = await this._readFileAsArrayBuffer(file);
      
      const encryptedBuffer = await window.crypto.subtle.encrypt(
        {
          name: "AES-GCM",
          iv,
          tagLength: 128 // Taille du tag d'authentification en bits
        },
        key,
        fileBuffer
      );
      
      // Créer un blob avec le fichier chiffré
      const encryptedFile = new Blob([encryptedBuffer], { type: 'application/octet-stream' });
      
      // Convertir le sel et l'IV en Base64 pour le stockage
      const saltBase64 = this._arrayBufferToBase64(salt);
      const ivBase64 = this._arrayBufferToBase64(iv);
      
      return { 
        encryptedFile, 
        salt: saltBase64, 
        iv: ivBase64 
      };
    } catch (error) {
      console.error("Erreur lors du chiffrement:", error);
      throw new Error(`Échec du chiffrement: ${error.message}`);
    }
  }

  // Déchiffrement de données
  async decryptData(encryptedData, saltBase64, ivBase64) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    try {
      const salt = this._base64ToArrayBuffer(saltBase64);
      const iv = ivBase64 ? this._base64ToArrayBuffer(ivBase64) : this._deriveIVFromSalt(salt); // Compatibilité avec l'ancien format
      const key = await this.deriveKey(this.userPassword, salt);
      
      const decryptedBuffer = await window.crypto.subtle.decrypt(
        {
          name: "AES-GCM",
          iv,
          tagLength: 128 // Taille du tag d'authentification en bits
        },
        key,
        encryptedData instanceof ArrayBuffer ? encryptedData : new Uint8Array(encryptedData)
      );
      
      return decryptedBuffer;
    } catch (error) {
      console.error("Erreur lors du déchiffrement:", error);
      if (error.name === "OperationError") {
        throw new Error("Échec du déchiffrement: le mot de passe est peut-être incorrect ou les données sont corrompues");
      }
      throw new Error(`Échec du déchiffrement: ${error.message}`);
    }
  }

  // Méthode de compatibilité pour l'ancien format (à supprimer après migration)
  _deriveIVFromSalt(salt) {
    console.warn("Utilisation de l'ancienne méthode de dérivation d'IV (compatibilité)");
    const ivSalt = new Uint8Array(salt);
    for (let i = 0; i < ivSalt.length; i++) {
      ivSalt[i] = (ivSalt[i] + 1) % 256;
    }
    return ivSalt.slice(0, this.ivLength);
  }

  // Gestion de la clé de chiffrement principale
  async importEncryptionKeyFromJwk(jwk) {
    try {
      this.encryptionKey = await window.crypto.subtle.importKey(
        "jwk",
        jwk,
        { 
          name: "AES-GCM",
          length: 256
        },
        false,
        ["encrypt", "decrypt"]
      );
      return this.encryptionKey;
    } catch (error) {
      console.error("Erreur lors de l'importation de la clé JWK:", error);
      throw new Error(`Échec de l'importation de la clé: ${error.message}`);
    }
  }

  async importEncryptionKeyFromBase64(base64Key) {
    try {
      const keyBytes = this._base64ToArrayBuffer(base64Key);
      this.encryptionKey = await window.crypto.subtle.importKey(
        "raw",
        keyBytes,
        { 
          name: "AES-GCM",
          length: 256
        },
        false,
        ["encrypt", "decrypt"]
      );
      return this.encryptionKey;
    } catch (error) {
      console.error("Erreur lors de l'importation de la clé Base64:", error);
      throw new Error(`Échec de l'importation de la clé: ${error.message}`);
    }
  }

  async generateEncryptionKey() {
    try {
      this.encryptionKey = await window.crypto.subtle.generateKey(
        {
          name: "AES-GCM",
          length: 256
        },
        true,
        ["encrypt", "decrypt"]
      );
      return this.encryptionKey;
    } catch (error) {
      console.error("Erreur lors de la génération de la clé:", error);
      throw new Error(`Échec de la génération de la clé: ${error.message}`);
    }
  }

  async ensureEncryptionKey() {
    if (!this.encryptionKey) {
      await this.generateEncryptionKey();
    }
    return this.encryptionKey;
  }

  // Chiffrement du mot de passe pour stockage temporaire
  async encryptPassword(password, encryptionKey) {
    try {
      const encoder = new TextEncoder();
      const passwordBuffer = encoder.encode(password);
      const iv = window.crypto.getRandomValues(new Uint8Array(12));
      
      const encryptedBuffer = await window.crypto.subtle.encrypt(
        {
          name: "AES-GCM",
          iv,
          tagLength: 128
        },
        encryptionKey,
        passwordBuffer
      );
      
      const encryptedPassword = this._arrayBufferToBase64(encryptedBuffer);
      const ivBase64 = this._arrayBufferToBase64(iv);
      const timestamp = Date.now().toString();
      return `${encryptedPassword}:${ivBase64}:${timestamp}`;
    } catch (error) {
      console.error("Erreur lors du chiffrement du mot de passe:", error);
      throw new Error(`Échec du chiffrement du mot de passe: ${error.message}`);
    }
  }

  // Déchiffrement du mot de passe
  async decryptPassword(encryptedPasswordData, encryptionKey) {
    try {
      const parts = encryptedPasswordData.split(':');
      if (parts.length < 2) {
        throw new Error("Format de mot de passe chiffré invalide");
      }
      
      const encryptedPasswordBase64 = parts[0];
      const ivBase64 = parts[1];
      const timestamp = parts.length > 2 ? parseInt(parts[2]) : null;
      
      // Vérifier l'expiration si un timestamp est présent
      if (timestamp && (Date.now() - timestamp > this.passwordExpiration)) {
        throw new Error("Le mot de passe stocké a expiré");
      }
      
      const encryptedBuffer = this._base64ToArrayBuffer(encryptedPasswordBase64);
      const iv = this._base64ToArrayBuffer(ivBase64);
      
      const decryptedBuffer = await window.crypto.subtle.decrypt(
        {
          name: "AES-GCM",
          iv,
          tagLength: 128
        },
        encryptionKey,
        encryptedBuffer
      );
      
      const decoder = new TextDecoder();
      return decoder.decode(decryptedBuffer);
    } catch (error) {
      console.error("Erreur lors du déchiffrement du mot de passe:", error);
      sessionStorage.removeItem('userPassword'); // Supprimer le mot de passe en cas d'erreur
      throw new Error("Impossible de récupérer le mot de passe stocké");
    }
  }

  // Mémorisation du mot de passe
  async rememberPassword(rememberPassword) {
    if (!this.hasUserPassword()) return;

    try {
      await this.ensureEncryptionKey();
      
      if (rememberPassword) {
        const encryptedPassword = await this.encryptPassword(this.userPassword, this.encryptionKey);
        sessionStorage.setItem('userPassword', encryptedPassword);
        console.log("Mot de passe utilisateur enregistré dans sessionStorage (expirera dans 30 minutes)");
      } else {
        sessionStorage.removeItem('userPassword');
      }
    } catch (error) {
      console.error("Erreur lors de la mémorisation du mot de passe:", error);
      sessionStorage.removeItem('userPassword'); // Par sécurité
    }
  }

  // Récupération du mot de passe
  async retrievePassword() {
    try {
      const encryptedPassword = sessionStorage.getItem('userPassword');
      if (!encryptedPassword) return null;
      
      await this.ensureEncryptionKey();
      return await this.decryptPassword(encryptedPassword, this.encryptionKey);
    } catch (error) {
      console.error("Erreur lors de la récupération du mot de passe:", error);
      sessionStorage.removeItem('userPassword'); // Supprimer en cas d'erreur
      return null;
    }
  }

  // Méthodes utilitaires
  _arrayBufferToBase64(buffer) {
    const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  }

  _base64ToArrayBuffer(base64) {
    const binaryString = window.atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  }

  _readFileAsArrayBuffer(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => reject(reader.error);
      reader.readAsArrayBuffer(file);
    });
  }
}

export default new PBKDF2CryptoService();