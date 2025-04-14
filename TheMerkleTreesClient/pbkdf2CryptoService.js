// pbkdf2CryptoService.js
const cryptoSubtle = window.crypto.subtle;
const cryptoRandom = window.crypto;

// Constantes cryptographiques
const CRYPTO_CONFIG = {
  SALT_SIZE: 16,
  IV_SIZE: 12,
  PBKDF2_ITERATIONS: 310000, // Augmenté selon recommandation OWASP 2023
  HASH_ALGORITHM: 'SHA-256',
  KEY_LENGTH: 256,
  PASSWORD_EXPIRY_MS: 30 * 60 * 1000, // 30 minutes
  PASSWORD_MIN_LENGTH: 8,
  STORAGE_KEY: 'encryptedAuthData'
};

const pbkdf2CryptoService = {
  userPassword: null,
  passwordTimestamp: null,
  derivedKeyCache: null,
  saltCache: null,
  masterKey: null,

  // Validation du mot de passe
  validatePassword(password) {
    if (!password || password.length < CRYPTO_CONFIG.PASSWORD_MIN_LENGTH) {
      throw new Error(`Le mot de passe doit contenir au moins ${CRYPTO_CONFIG.PASSWORD_MIN_LENGTH} caractères`);
    }
    return true;
  },

  // Méthodes de gestion du mot de passe
  setUserPassword(password) {
    if (this.validatePassword(password)) {
      this.userPassword = password;
      this.passwordTimestamp = Date.now();
      this.derivedKeyCache = null; // Invalider le cache de clé
      this.saltCache = null;
      
      // Générer une clé maître pour cette session
      this._generateMasterKey();
    }
  },

  getUserPassword() {
    // Vérifier si le mot de passe a expiré
    if (this.passwordTimestamp && 
        (Date.now() - this.passwordTimestamp > CRYPTO_CONFIG.PASSWORD_EXPIRY_MS)) {
      this.clearUserPassword();
      return null;
    }
    return this.userPassword;
  },

  hasUserPassword() {
    return this.getUserPassword() !== null;
  },

  clearUserPassword() {
    this.userPassword = null;
    this.passwordTimestamp = null;
    this.derivedKeyCache = null;
    this.saltCache = null;
    this.masterKey = null;
    sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
  },

  // Génère une clé maître pour chiffrer les données en session
  async _generateMasterKey() {
    try {
      this.masterKey = await cryptoSubtle.generateKey(
        {
          name: 'AES-GCM',
          length: CRYPTO_CONFIG.KEY_LENGTH
        },
        true, // extractable
        ['encrypt', 'decrypt']
      );
      return this.masterKey;
    } catch (error) {
      console.error('Erreur lors de la génération de la clé maître:', error);
      throw error;
    }
  },

  // Chiffre les données avec la clé maître
  async _encryptWithMasterKey(data) {
    if (!this.masterKey) {
      await this._generateMasterKey();
    }
    
    const iv = cryptoRandom.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(JSON.stringify(data));
    
    const encryptedData = await cryptoSubtle.encrypt(
      { name: 'AES-GCM', iv },
      this.masterKey,
      dataBuffer
    );
    
    // Exporter la clé maître pour pouvoir la stocker
    const exportedKey = await cryptoSubtle.exportKey('raw', this.masterKey);
    
    return {
      encryptedData: this.arrayBufferToBase64(encryptedData),
      iv: this.arrayBufferToBase64(iv),
      key: this.arrayBufferToBase64(exportedKey)
    };
  },

  // Déchiffre les données avec une clé exportée
  async _decryptWithExportedKey(encryptedDataBase64, ivBase64, exportedKeyBase64) {
    const encryptedData = this.base64ToArrayBuffer(encryptedDataBase64);
    const iv = this.base64ToArrayBuffer(ivBase64);
    const exportedKey = this.base64ToArrayBuffer(exportedKeyBase64);
    
    // Importer la clé
    const key = await cryptoSubtle.importKey(
      'raw',
      exportedKey,
      { name: 'AES-GCM', length: CRYPTO_CONFIG.KEY_LENGTH },
      false,
      ['decrypt']
    );
    
    const decryptedBuffer = await cryptoSubtle.decrypt(
      { name: 'AES-GCM', iv },
      key,
      encryptedData
    );
    
    const decoder = new TextDecoder();
    const decryptedText = decoder.decode(decryptedBuffer);
    
    return JSON.parse(decryptedText);
  },

  async rememberPassword(remember) {
    if (remember && this.userPassword) {
      try {
        // Chiffrer le mot de passe et le timestamp avant de les stocker
        const dataToStore = {
          password: this.userPassword,
          timestamp: this.passwordTimestamp
        };
        
        const encryptedData = await this._encryptWithMasterKey(dataToStore);
        sessionStorage.setItem(CRYPTO_CONFIG.STORAGE_KEY, JSON.stringify(encryptedData));
      } catch (error) {
        console.error('Erreur lors du stockage sécurisé du mot de passe:', error);
        // En cas d'erreur, ne pas stocker le mot de passe
        sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
      }
    } else {
      sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
    }
  },

  async retrievePassword() {
    const storedData = sessionStorage.getItem(CRYPTO_CONFIG.STORAGE_KEY);
    
    if (storedData) {
      try {
        const { encryptedData, iv, key } = JSON.parse(storedData);
        
        // Déchiffrer les données stockées
        const decryptedData = await this._decryptWithExportedKey(encryptedData, iv, key);
        const { password, timestamp } = decryptedData;
        
        // Vérifier si le mot de passe a expiré
        if (Date.now() - timestamp > CRYPTO_CONFIG.PASSWORD_EXPIRY_MS) {
          sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
          return null;
        }
        
        // Restaurer le mot de passe et le timestamp
        this.userPassword = password;
        this.passwordTimestamp = timestamp;
        
        // Importer la clé maître
        this.masterKey = await cryptoSubtle.importKey(
          'raw',
          this.base64ToArrayBuffer(key),
          { name: 'AES-GCM', length: CRYPTO_CONFIG.KEY_LENGTH },
          true,
          ['encrypt', 'decrypt']
        );
        
        return password;
      } catch (error) {
        console.error('Erreur lors de la récupération du mot de passe:', error);
        sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
        return null;
      }
    }
    return null;
  },

  // Utilitaires de conversion
  arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  },

  base64ToArrayBuffer(base64) {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  },

  // Dériver une clé à partir d'un mot de passe et d'un sel
  async deriveKey(password, salt, useCache = false) {
    // Utiliser le cache si demandé et disponible avec le même sel
    if (useCache && this.derivedKeyCache && this.saltCache && 
        this.arrayBufferEquals(salt, this.saltCache)) {
      return this.derivedKeyCache;
    }

    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);

    // Importer le mot de passe comme clé brute
    const importedKey = await cryptoSubtle.importKey(
      'raw',
      passwordBuffer,
      { name: 'PBKDF2' },
      false,
      ['deriveKey']
    );

    // Dériver une clé AES-GCM à partir du mot de passe
    const key = await cryptoSubtle.deriveKey(
      {
        name: 'PBKDF2',
        salt: salt,
        iterations: CRYPTO_CONFIG.PBKDF2_ITERATIONS,
        hash: CRYPTO_CONFIG.HASH_ALGORITHM
      },
      importedKey,
      { name: 'AES-GCM', length: CRYPTO_CONFIG.KEY_LENGTH },
      true, // extractable pour pouvoir l'utiliser avec CryptoKey API
      ['encrypt', 'decrypt']
    );

    // Mettre en cache si demandé
    if (useCache) {
      this.derivedKeyCache = key;
      this.saltCache = salt;
    }

    return key;
  },

  // Comparer deux ArrayBuffers
  arrayBufferEquals(buf1, buf2) {
    if (buf1 === buf2) return true;
    if (buf1.byteLength !== buf2.byteLength) return false;
    
    const dv1 = new Uint8Array(buf1);
    const dv2 = new Uint8Array(buf2);
    
    for (let i = 0; i < buf1.byteLength; i++) {
      if (dv1[i] !== dv2[i]) return false;
    }
    return true;
  },

  // Chiffrer un fichier en utilisant CryptoKey
  async encryptFile(file) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini");
    }

    // Générer un sel aléatoire
    const salt = cryptoRandom.getRandomValues(new Uint8Array(CRYPTO_CONFIG.SALT_SIZE));

    // Générer un IV aléatoire
    const iv = cryptoRandom.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));

    // Dériver la clé
    const key = await this.deriveKey(this.userPassword, salt, true);

    // Lire le fichier
    const fileBuffer = await file.arrayBuffer();
    
    // Chiffrer le fichier
    const encryptedBuffer = await cryptoSubtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      fileBuffer
    );

    // Créer un nouveau fichier avec les données chiffrées
    const encryptedFile = new File(
      [encryptedBuffer],
      file.name,
      { type: 'application/octet-stream' }
    );

    return {
      encryptedFile,
      salt: this.arrayBufferToBase64(salt),
      iv: this.arrayBufferToBase64(iv)
    };
  },

  // Déchiffrer des données en utilisant CryptoKey
  async decryptData(encryptedData, saltBase64, ivBase64) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    // Convertir le sel et l'IV de base64 en ArrayBuffer
    const salt = this.base64ToArrayBuffer(saltBase64);
    const iv = this.base64ToArrayBuffer(ivBase64);

    // Dériver la clé (utiliser le cache si possible)
    const key = await this.deriveKey(this.userPassword, salt, true);

    try {
      // Déchiffrer les données
      const decryptedBuffer = await cryptoSubtle.decrypt(
        { name: 'AES-GCM', iv },
        key,
        encryptedData
      );

      return decryptedBuffer;
    } catch (error) {
      throw new Error("Erreur lors du déchiffrement: " + error.message);
    }
  },

  // Chiffrer les métadonnées en utilisant CryptoKey
  async encryptMetadata(metadata) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    // Générer un sel et un IV uniques pour les métadonnées
    const salt = cryptoRandom.getRandomValues(new Uint8Array(CRYPTO_CONFIG.SALT_SIZE));
    const iv = cryptoRandom.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));

    // Dériver la clé à partir du mot de passe et du sel
    const key = await this.deriveKey(this.userPassword, salt, false);

    // Convertir les métadonnées en JSON puis en ArrayBuffer
    const metadataString = JSON.stringify(metadata);
    const encoder = new TextEncoder();
    const metadataBuffer = encoder.encode(metadataString);

    // Chiffrer les métadonnées
    const encryptedBuffer = await cryptoSubtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      metadataBuffer
    );

    // Convertir en base64 pour le stockage/transmission
    const encryptedBase64 = this.arrayBufferToBase64(encryptedBuffer);

    return {
      encryptedMetadata: encryptedBase64,
      metadataSalt: this.arrayBufferToBase64(salt),
      metadataIV: this.arrayBufferToBase64(iv)
    };
  },

  // Déchiffrer les métadonnées en utilisant CryptoKey
  async decryptMetadata(encryptedMetadata, metadataSalt, metadataIV) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    // Convertir de base64 à ArrayBuffer
    const encryptedBuffer = this.base64ToArrayBuffer(encryptedMetadata);
    const saltBuffer = this.base64ToArrayBuffer(metadataSalt);
    const ivBuffer = this.base64ToArrayBuffer(metadataIV);

    // Dériver la clé (sans utiliser le cache car sel différent)
    const key = await this.deriveKey(this.userPassword, saltBuffer, false);

    try {
      // Déchiffrer
      const decryptedBuffer = await cryptoSubtle.decrypt(
        { name: 'AES-GCM', iv: ivBuffer },
        key,
        encryptedBuffer
      );

      // Convertir en chaîne JSON puis parser
      const decoder = new TextDecoder();
      const decryptedString = decoder.decode(decryptedBuffer);

      return JSON.parse(decryptedString);
    } catch (error) {
      throw new Error("Erreur lors du déchiffrement des métadonnées: " + error.message);
    }
  },

  // Méthode pour gérer les erreurs de chiffrement/déchiffrement
  handleCryptoError(error) {
    console.error("Erreur cryptographique:", error);
    
    if (error.message && error.message.includes("déchiffrement")) {
      // Problème probable de mot de passe incorrect
      this.clearUserPassword();
      return {
        type: "password_error",
        message: "Erreur de déchiffrement. Le mot de passe est peut-être incorrect ou a expiré."
      };
    }
    
    return {
      type: "crypto_error",
      message: "Une erreur est survenue lors de l'opération cryptographique: " + error.message
    };
  }
};

export default pbkdf2CryptoService;