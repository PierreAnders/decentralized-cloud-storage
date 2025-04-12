/**
 * Service de chiffrement basé sur mot de passe pour l'application décentralisée
 * Utilise PBKDF2 et Web Crypto API pour dériver des clés à partir du mot de passe utilisateur
 */

class PBKDF2CryptoService {
  constructor() {
    // Paramètres pour PBKDF2
    this.iterations = 100000; // Nombre d'itérations pour PBKDF2
    this.keyLength = 256; // Longueur de la clé en bits
    this.ivLength = 16; // Longueur du vecteur d'initialisation en octets
    this.saltLength = 16; // Longueur du sel en octets
    
    // Stockage temporaire du mot de passe en mémoire
    this.userPassword = null;
  }

  /**
   * Définit le mot de passe utilisateur pour la session
   * @param {string} password - Mot de passe de l'utilisateur
   */
  setUserPassword(password) {
    this.userPassword = password;
  }

  /**
   * Efface le mot de passe de la mémoire
   */
  clearUserPassword() {
    this.userPassword = null;
  }

  /**
   * Vérifie si un mot de passe est défini
   * @returns {boolean} Vrai si un mot de passe est défini
   */
  hasUserPassword() {
    return this.userPassword !== null && this.userPassword !== undefined;
  }

  /**
   * Génère un sel aléatoire
   * @returns {Uint8Array} Sel aléatoire
   */
  generateSalt() {
    return window.crypto.getRandomValues(new Uint8Array(this.saltLength));
  }

  /**
   * Dérive une clé et un IV à partir du mot de passe et du sel
   * @param {string} password - Mot de passe utilisateur
   * @param {Uint8Array} salt - Sel pour la dérivation
   * @returns {Promise<{key: CryptoKey, iv: Uint8Array}>} Clé et IV dérivés
   */
  async deriveKeyAndIV(password, salt) {
    // Convertir le mot de passe en ArrayBuffer
    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);
    
    // Dériver une clé intermédiaire avec PBKDF2
    const baseKey = await window.crypto.subtle.importKey(
      "raw",
      passwordBuffer,
      { name: "PBKDF2" },
      false,
      ["deriveBits", "deriveKey"]
    );
    
    // Dériver la clé AES
    const key = await window.crypto.subtle.deriveKey(
      {
        name: "PBKDF2",
        salt: salt,
        iterations: this.iterations,
        hash: "SHA-256"
      },
      baseKey,
      {
        name: "AES-CBC",
        length: this.keyLength
      },
      true, // extractable
      ["encrypt", "decrypt"]
    );
    
    // Dériver l'IV (en utilisant une partie différente du sel)
    const ivSalt = new Uint8Array(salt);
    // Modifier légèrement le sel pour l'IV pour éviter de réutiliser exactement le même sel
    for (let i = 0; i < ivSalt.length; i++) {
      ivSalt[i] = (ivSalt[i] + 1) % 256;
    }
    
    const ivBits = await window.crypto.subtle.deriveBits(
      {
        name: "PBKDF2",
        salt: ivSalt,
        iterations: this.iterations / 10, // Moins d'itérations pour l'IV
        hash: "SHA-256"
      },
      baseKey,
      this.ivLength * 8 // Longueur en bits
    );
    
    const iv = new Uint8Array(ivBits).slice(0, this.ivLength);
    
    return { key, iv };
  }

  /**
   * Chiffre un fichier avec une clé dérivée du mot de passe
   * @param {File} file - Le fichier à chiffrer
   * @returns {Promise<{encryptedFile: Blob, salt: string}>} Fichier chiffré et sel encodé en Base64
   */
  async encryptFile(file) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini");
    }
    
    try {
      // Génération d'un sel aléatoire
      const salt = this.generateSalt();
      
      // Dérivation de la clé et de l'IV à partir du mot de passe et du sel
      const { key, iv } = await this.deriveKeyAndIV(this.userPassword, salt);
      
      // Lecture du fichier en ArrayBuffer
      const fileBuffer = await this._readFileAsArrayBuffer(file);
      
      // Chiffrement du fichier
      const encryptedBuffer = await window.crypto.subtle.encrypt(
        {
          name: "AES-CBC",
          iv
        },
        key,
        fileBuffer
      );
      
      // Conversion en Blob pour l'envoi
      const encryptedFile = new Blob([encryptedBuffer], { type: 'application/octet-stream' });
      
      // Conversion du sel en Base64 pour stockage
      const saltBase64 = this._arrayBufferToBase64(salt);
      
      return { encryptedFile, salt: saltBase64 };
    } catch (error) {
      console.error("Erreur lors du chiffrement:", error);
      throw error;
    }
  }

  /**
   * Déchiffre des données avec une clé dérivée du mot de passe
   * @param {ArrayBuffer} encryptedData - Les données chiffrées
   * @param {string} saltBase64 - Le sel encodé en Base64
   * @returns {Promise<ArrayBuffer>} Les données déchiffrées
   */
  async decryptData(encryptedData, saltBase64) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini");
    }
    
    try {
      // Conversion du sel depuis Base64
      const salt = this._base64ToArrayBuffer(saltBase64);
      
      // Dérivation de la clé et de l'IV à partir du mot de passe et du sel
      const { key, iv } = await this.deriveKeyAndIV(this.userPassword, salt);
      
      // Déchiffrement des données
      const decryptedBuffer = await window.crypto.subtle.decrypt(
        {
          name: "AES-CBC",
          iv
        },
        key,
        encryptedData
      );
      
      return decryptedBuffer;
    } catch (error) {
      console.error("Erreur lors du déchiffrement:", error);
      throw error;
    }
  }

  /**
   * Stocke le mot de passe dans sessionStorage (optionnel)
   * @param {boolean} rememberPassword - Si vrai, stocke le mot de passe chiffré
   */
  rememberPassword(rememberPassword) {
    if (!this.hasUserPassword()) return;
    
    if (rememberPassword) {
      // Stockage simple pour la démonstration - dans une implémentation réelle,
      // il faudrait chiffrer le mot de passe avant de le stocker
      sessionStorage.setItem('userPassword', this.userPassword);
    } else {
      sessionStorage.removeItem('userPassword');
    }
  }

  /**
   * Récupère le mot de passe depuis sessionStorage (si disponible)
   * @returns {string|null} Le mot de passe stocké ou null
   */
  retrievePassword() {
    return sessionStorage.getItem('userPassword');
  }

  /**
   * Convertit un ArrayBuffer en chaîne Base64
   * @private
   * @param {ArrayBuffer|Uint8Array} buffer - Le buffer à convertir
   * @returns {string} La chaîne Base64
   */
  _arrayBufferToBase64(buffer) {
    const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  }

  /**
   * Convertit une chaîne Base64 en ArrayBuffer
   * @private
   * @param {string} base64 - La chaîne Base64 à convertir
   * @returns {ArrayBuffer} Le buffer résultant
   */
  _base64ToArrayBuffer(base64) {
    const binaryString = window.atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  }

  /**
   * Lit un fichier en tant qu'ArrayBuffer
   * @private
   * @param {File} file - Le fichier à lire
   * @returns {Promise<ArrayBuffer>} Le contenu du fichier
   */
  _readFileAsArrayBuffer(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => reject(reader.error);
      reader.readAsArrayBuffer(file);
    });
  }
}

// Export du service pour utilisation dans les composants Vue
export default new PBKDF2CryptoService();
