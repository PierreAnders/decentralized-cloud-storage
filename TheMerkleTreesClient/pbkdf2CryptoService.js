// pbkdf2CryptoService.js

// Constantes cryptographiques
const CRYPTO_CONFIG = {
  SALT_SIZE: 16,
  IV_SIZE: 12,
  PBKDF2_ITERATIONS: 310000,
  HASH_ALGORITHM: 'SHA-256',
  KEY_LENGTH: 256,
  PASSWORD_EXPIRY_MS: 30 * 60 * 1000,
  PASSWORD_MIN_LENGTH: 8,
  STORAGE_KEY: 'encryptedAuthData'
};

const pbkdf2CryptoService = {
  userPassword: null,
  passwordTimestamp: null,
  derivedKeyCache: null,
  saltCache: null,
  masterKey: null,

  // Méthode pour accéder de manière sécurisée à l'API Crypto
  getCrypto() {
    if (typeof window === 'undefined' || !window.crypto) {
      throw new Error('Crypto API non disponible');
    }
    return {
      subtle: window.crypto.subtle,
      random: window.crypto
    };
  },

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
      this.derivedKeyCache = null;
      this.saltCache = null;
      this._generateMasterKey();
    }
  },

  getUserPassword() {
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
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
    }
  },

  async _generateMasterKey() {
    try {
      const { subtle } = this.getCrypto();
      this.masterKey = await subtle.generateKey(
        {
          name: 'AES-GCM',
          length: CRYPTO_CONFIG.KEY_LENGTH
        },
        true,
        ['encrypt', 'decrypt']
      );
      return this.masterKey;
    } catch (error) {
      console.error('Erreur lors de la génération de la clé maître:', error);
      throw error;
    }
  },

  async _encryptWithMasterKey(data) {
    if (!this.masterKey) {
      await this._generateMasterKey();
    }
    
    const { subtle, random } = this.getCrypto();
    const iv = random.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(JSON.stringify(data));
    
    const encryptedData = await subtle.encrypt(
      { name: 'AES-GCM', iv },
      this.masterKey,
      dataBuffer
    );
    
    const exportedKey = await subtle.exportKey('raw', this.masterKey);
    
    return {
      encryptedData: this.arrayBufferToBase64(encryptedData),
      iv: this.arrayBufferToBase64(iv),
      key: this.arrayBufferToBase64(exportedKey)
    };
  },

  async _decryptWithExportedKey(encryptedDataBase64, ivBase64, exportedKeyBase64) {
    const { subtle } = this.getCrypto();
    const encryptedData = this.base64ToArrayBuffer(encryptedDataBase64);
    const iv = this.base64ToArrayBuffer(ivBase64);
    const exportedKey = this.base64ToArrayBuffer(exportedKeyBase64);
    
    const key = await subtle.importKey(
      'raw',
      exportedKey,
      { name: 'AES-GCM', length: CRYPTO_CONFIG.KEY_LENGTH },
      false,
      ['decrypt']
    );
    
    const decryptedBuffer = await subtle.decrypt(
      { name: 'AES-GCM', iv },
      key,
      encryptedData
    );
    
    const decoder = new TextDecoder();
    const decryptedText = decoder.decode(decryptedBuffer);
    
    return JSON.parse(decryptedText);
  },

  async rememberPassword(remember) {
    if (remember && this.userPassword && typeof window !== 'undefined') {
      try {
        const dataToStore = {
          password: this.userPassword,
          timestamp: this.passwordTimestamp
        };
        
        const encryptedData = await this._encryptWithMasterKey(dataToStore);
        sessionStorage.setItem(CRYPTO_CONFIG.STORAGE_KEY, JSON.stringify(encryptedData));
      } catch (error) {
        console.error('Erreur lors du stockage sécurisé du mot de passe:', error);
        sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
      }
    } else if (typeof window !== 'undefined') {
      sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
    }
  },

  async retrievePassword() {
    if (typeof window === 'undefined') return null;
    
    const storedData = sessionStorage.getItem(CRYPTO_CONFIG.STORAGE_KEY);
    
    if (storedData) {
      try {
        const { encryptedData, iv, key } = JSON.parse(storedData);
        const decryptedData = await this._decryptWithExportedKey(encryptedData, iv, key);
        const { password, timestamp } = decryptedData;
        
        if (Date.now() - timestamp > CRYPTO_CONFIG.PASSWORD_EXPIRY_MS) {
          sessionStorage.removeItem(CRYPTO_CONFIG.STORAGE_KEY);
          return null;
        }
        
        this.userPassword = password;
        this.passwordTimestamp = timestamp;
        
        const { subtle } = this.getCrypto();
        this.masterKey = await subtle.importKey(
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

  async deriveKey(password, salt, useCache = false) {
    const { subtle } = this.getCrypto();
    
    if (useCache && this.derivedKeyCache && this.saltCache && 
        this.arrayBufferEquals(salt, this.saltCache)) {
      return this.derivedKeyCache;
    }

    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);

    const importedKey = await subtle.importKey(
      'raw',
      passwordBuffer,
      { name: 'PBKDF2' },
      false,
      ['deriveKey']
    );

    const key = await subtle.deriveKey(
      {
        name: 'PBKDF2',
        salt: salt,
        iterations: CRYPTO_CONFIG.PBKDF2_ITERATIONS,
        hash: CRYPTO_CONFIG.HASH_ALGORITHM
      },
      importedKey,
      { name: 'AES-GCM', length: CRYPTO_CONFIG.KEY_LENGTH },
      true,
      ['encrypt', 'decrypt']
    );

    if (useCache) {
      this.derivedKeyCache = key;
      this.saltCache = salt;
    }

    return key;
  },

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

  async encryptFile(file) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini");
    }

    const { subtle, random } = this.getCrypto();
    const salt = random.getRandomValues(new Uint8Array(CRYPTO_CONFIG.SALT_SIZE));
    const iv = random.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));

    const key = await this.deriveKey(this.userPassword, salt, true);
    const fileBuffer = await file.arrayBuffer();
    
    const encryptedBuffer = await subtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      fileBuffer
    );

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

  async decryptData(encryptedData, saltBase64, ivBase64) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    const { subtle } = this.getCrypto();
    const salt = this.base64ToArrayBuffer(saltBase64);
    const iv = this.base64ToArrayBuffer(ivBase64);

    const key = await this.deriveKey(this.userPassword, salt, true);

    try {
      const decryptedBuffer = await subtle.decrypt(
        { name: 'AES-GCM', iv },
        key,
        encryptedData
      );

      return decryptedBuffer;
    } catch (error) {
      throw new Error("Erreur lors du déchiffrement: " + error.message);
    }
  },

  async encryptMetadata(metadata) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    const { subtle, random } = this.getCrypto();
    const salt = random.getRandomValues(new Uint8Array(CRYPTO_CONFIG.SALT_SIZE));
    const iv = random.getRandomValues(new Uint8Array(CRYPTO_CONFIG.IV_SIZE));

    const key = await this.deriveKey(this.userPassword, salt, false);
    const metadataString = JSON.stringify(metadata);
    const encoder = new TextEncoder();
    const metadataBuffer = encoder.encode(metadataString);

    const encryptedBuffer = await subtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      metadataBuffer
    );

    const encryptedBase64 = this.arrayBufferToBase64(encryptedBuffer);

    return {
      encryptedMetadata: encryptedBase64,
      metadataSalt: this.arrayBufferToBase64(salt),
      metadataIV: this.arrayBufferToBase64(iv)
    };
  },

  async decryptMetadata(encryptedMetadata, metadataSalt, metadataIV) {
    if (!this.hasUserPassword()) {
      throw new Error("Mot de passe utilisateur non défini ou expiré");
    }

    const { subtle } = this.getCrypto();
    const encryptedBuffer = this.base64ToArrayBuffer(encryptedMetadata);
    const saltBuffer = this.base64ToArrayBuffer(metadataSalt);
    const ivBuffer = this.base64ToArrayBuffer(metadataIV);

    const key = await this.deriveKey(this.userPassword, saltBuffer, false);

    try {
      const decryptedBuffer = await subtle.decrypt(
        { name: 'AES-GCM', iv: ivBuffer },
        key,
        encryptedBuffer
      );

      const decoder = new TextDecoder();
      const decryptedString = decoder.decode(decryptedBuffer);

      return JSON.parse(decryptedString);
    } catch (error) {
      throw new Error("Erreur lors du déchiffrement des métadonnées: " + error.message);
    }
  },

  handleCryptoError(error) {
    console.error("Erreur cryptographique:", error);
    
    if (error.message && error.message.includes("déchiffrement")) {
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