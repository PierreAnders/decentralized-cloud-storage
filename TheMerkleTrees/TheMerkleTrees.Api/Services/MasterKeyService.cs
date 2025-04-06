using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace TheMerkleTrees.Api.Services
{
    /// <summary>
    /// Interface pour le service de gestion des clés maîtresses
    /// </summary>
    public interface IMasterKeyService
    {
        /// <summary>
        /// Stocke une clé maîtresse pour un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="masterKey">Clé maîtresse</param>
        /// <param name="expiresAt">Date d'expiration</param>
        void StoreKey(string userId, byte[] masterKey, DateTime expiresAt);
        
        /// <summary>
        /// Récupère la clé maîtresse d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <returns>Clé maîtresse ou null si non trouvée</returns>
        byte[] GetKey(string userId);
        
        /// <summary>
        /// Vérifie si une clé maîtresse existe pour un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <returns>True si la clé existe, false sinon</returns>
        bool HasKey(string userId);
        
        /// <summary>
        /// Supprime la clé maîtresse d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        void RemoveKey(string userId);
    }

    /// <summary>
    /// Service de gestion des clés maîtresses en mémoire
    /// </summary>
    public class MasterKeyService : IMasterKeyService, IDisposable
    {
        private readonly ConcurrentDictionary<string, (byte[] Key, DateTime ExpiresAt)> _keys = new();
        private readonly Timer _cleanupTimer;
        private readonly ILogger<MasterKeyService> _logger;
        
        /// <summary>
        /// Constructeur du service
        /// </summary>
        /// <param name="logger">Logger</param>
        public MasterKeyService(ILogger<MasterKeyService> logger)
        {
            _logger = logger;
            // Configurer un timer pour nettoyer les clés expirées toutes les 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredKeys, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }
        
        /// <summary>
        /// Stocke une clé maîtresse pour un utilisateur
        /// </summary>
        public void StoreKey(string userId, byte[] masterKey, DateTime expiresAt)
        {
            _keys[userId] = (masterKey, expiresAt);
            _logger.LogInformation("Clé maîtresse stockée pour l'utilisateur: {UserId}, expire à: {ExpiresAt}", userId, expiresAt);
        }
        
        /// <summary>
        /// Récupère la clé maîtresse d'un utilisateur
        /// </summary>
        public byte[] GetKey(string userId)
        {
            if (_keys.TryGetValue(userId, out var keyInfo))
            {
                if (DateTime.UtcNow <= keyInfo.ExpiresAt)
                {
                    return keyInfo.Key;
                }
                
                // La clé a expiré, la supprimer
                RemoveKey(userId);
                _logger.LogInformation("Tentative d'accès à une clé expirée pour l'utilisateur: {UserId}", userId);
            }
            
            return null;
        }
        
        /// <summary>
        /// Vérifie si une clé maîtresse existe pour un utilisateur
        /// </summary>
        public bool HasKey(string userId)
        {
            if (_keys.TryGetValue(userId, out var keyInfo))
            {
                if (DateTime.UtcNow <= keyInfo.ExpiresAt)
                {
                    return true;
                }
                
                // La clé a expiré, la supprimer
                RemoveKey(userId);
            }
            
            return false;
        }
        
        /// <summary>
        /// Supprime la clé maîtresse d'un utilisateur
        /// </summary>
        public void RemoveKey(string userId)
        {
            if (_keys.TryRemove(userId, out _))
            {
                _logger.LogInformation("Clé maîtresse supprimée pour l'utilisateur: {UserId}", userId);
            }
        }
        
        /// <summary>
        /// Nettoie les clés expirées
        /// </summary>
        private void CleanupExpiredKeys(object state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _keys.Where(kv => kv.Value.ExpiresAt < now).Select(kv => kv.Key).ToList();
            
            foreach (var key in expiredKeys)
            {
                RemoveKey(key);
            }
            
            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Nettoyage de {Count} clés maîtresses expirées", expiredKeys.Count);
            }
        }
        
        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
    
    /// <summary>
    /// Utilitaires pour la gestion des clés cryptographiques
    /// </summary>
    public static class CryptoUtils
    {
        /// <summary>
        /// Dérive une clé maîtresse à partir d'un mot de passe et d'un email
        /// </summary>
        /// <param name="password">Mot de passe</param>
        /// <param name="email">Email de l'utilisateur</param>
        /// <param name="salt">Sel supplémentaire</param>
        /// <param name="iterations">Nombre d'itérations</param>
        /// <returns>Clé dérivée (256 bits)</returns>
        public static byte[] DeriveKeyFromPassword(string password, string email, string salt, int iterations = 10000)
        {
            // Combiner l'email et le sel pour créer un sel unique par utilisateur
            byte[] saltBytes = Encoding.UTF8.GetBytes(email + salt);
            
            // Utiliser PBKDF2 pour dériver une clé à partir du mot de passe
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(32); // 256 bits
            }
        }
        
        /// <summary>
        /// Chiffre des données avec une clé maîtresse
        /// </summary>
        /// <param name="dataToEncrypt">Données à chiffrer</param>
        /// <param name="masterKey">Clé maîtresse</param>
        /// <returns>Données chiffrées en base64 (IV + données)</returns>
        public static string EncryptWithMasterKey(byte[] dataToEncrypt, byte[] masterKey)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = masterKey;
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Écrire l'IV en clair au début du flux
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                        csEncrypt.FlushFinalBlock();
                    }
                    
                    // Retourner l'IV + données chiffrées en base64
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        
        /// <summary>
        /// Déchiffre des données avec une clé maîtresse
        /// </summary>
        /// <param name="encryptedData">Données chiffrées en base64 (IV + données)</param>
        /// <param name="masterKey">Clé maîtresse</param>
        /// <returns>Données déchiffrées</returns>
        public static byte[] DecryptWithMasterKey(string encryptedData, byte[] masterKey)
        {
            byte[] cipherText = Convert.FromBase64String(encryptedData);
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = masterKey;
                
                // Extraire l'IV du début des données chiffrées
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                // Déchiffrer les données
                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream())
                {
                    using (var csDecrypt = new CryptoStream(
                        new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length),
                        decryptor, CryptoStreamMode.Read))
                    {
                        csDecrypt.CopyTo(msDecrypt);
                    }
                    
                    return msDecrypt.ToArray();
                }
            }
        }
    }
}
