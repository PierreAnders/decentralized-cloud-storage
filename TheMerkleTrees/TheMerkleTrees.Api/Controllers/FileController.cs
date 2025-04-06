using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using TheMerkleTrees.Api.Services;
using File = TheMerkleTrees.Domain.Models.File;

namespace TheMerkleTrees.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;
        private readonly HttpClient _httpClient;
        private readonly string _ipfsGatewayUrl;
        private readonly string _ipfsApiUrl;
        private readonly bool _isDevelopment;
        private readonly IMasterKeyService _masterKeyService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFileRepository mongoDbService, 
            HttpClient httpClient, 
            IConfiguration configuration,
            IMasterKeyService masterKeyService,
            ILogger<FilesController> logger)
        {
            _fileRepository = mongoDbService;
            _httpClient = httpClient;
            _ipfsGatewayUrl = configuration["Ipfs:GatewayUrl"];
            _ipfsApiUrl = configuration["Ipfs:ApiUrl"];
            _isDevelopment = bool.Parse(configuration["Environment:IsDevelopment"]);
            _masterKeyService = masterKeyService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post(File newFile)
        {
            await _fileRepository.CreateAsync(newFile);

            return CreatedAtAction(nameof(Post), new { id = newFile.Id }, newFile);
        }
        
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            await _fileRepository.RemoveAsync(name, userId);

            return NoContent();
        }

        [HttpGet("user")]
        public async Task<ActionResult<List<File>>> GetFilesByUser()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            var files = await _fileRepository.GetFilesByUserAsync(userId);

            return files;
        }
        
        [HttpGet("user/category/{category}")]
        public async Task<ActionResult<List<File>>> GetFilesByUserAndCategory(string category)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            var files = await _fileRepository.GetFilesByUserAndCategoryAsync(category, userId);

            return files;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(1073741824)] 
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string category,
            [FromForm] bool isPublic, [FromForm] string userAddress)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            
            // Vérifier si la clé maîtresse est disponible
            if (!isPublic && !_masterKeyService.HasKey(userId))
            {
                _logger.LogWarning("Tentative d'upload de fichier privé sans clé maîtresse pour l'utilisateur: {UserId}", userId);
                return BadRequest("Vous devez vous reconnecter pour pouvoir uploader des fichiers privés.");
            }

            byte[] fileContent;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileContent = ms.ToArray();
            }

            byte[] encryptedContent;
            string encryptedKey = null;
            string encryptedIv = null;

            if (!isPublic)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    
                    // Récupérer la clé maîtresse
                    byte[] masterKey = _masterKeyService.GetKey(userId);
                    
                    // Chiffrer la clé AES et l'IV avec la clé maîtresse
                    encryptedKey = CryptoUtils.EncryptWithMasterKey(aes.Key, masterKey);
                    encryptedIv = CryptoUtils.EncryptWithMasterKey(aes.IV, masterKey);
                    
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var msEncrypt = new MemoryStream())
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        await csEncrypt.WriteAsync(fileContent, 0, fileContent.Length);
                        await csEncrypt.FlushFinalBlockAsync();
                        encryptedContent = msEncrypt.ToArray();
                    }
                }
            }
            else
            {
                encryptedContent = fileContent;
            }
            
            var formData = new MultipartFormDataContent();
            formData.Add(new ByteArrayContent(encryptedContent), "file", file.FileName);
            
            var response = await _httpClient.PostAsync(_ipfsApiUrl, formData);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AddResponse>();
            var cid = result.Hash;
            var url = $"ipfs://{cid}";
            
            var fileRecord = new File
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = file.FileName,
                Hash = cid,
                Category = category,
                IsPublic = isPublic,
                Owner = userId,
                Key = encryptedKey,
                IV = encryptedIv,
                Extension = Path.GetExtension(file.FileName)
            };

            await _fileRepository.CreateAsync(fileRecord);

            return Ok(new { Message = "File uploaded successfully", Url = url });
        }

        [HttpGet("decrypt/{name}")]
        public async Task<IActionResult> DecryptFile(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            // var decodedName = Uri.UnescapeDataString(name);
            var file = await _fileRepository.GetAsync(name, userId);
            if (file == null)
            {
                return NotFound("Fichier non trouvé.");
            }

            byte[] fileContent = null;
            try
            {
                fileContent = await GetFileFromLocalIPFSNode(file.Hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération via le nœud IPFS pour le fichier: {FileName}", file.Name);
                return BadRequest("Impossible de récupérer le fichier depuis IPFS.");
            }

            if (file.IsPublic)
            {
                return File(fileContent, "application/octet-stream", file.Name);
            }
            
            // Vérifier si la clé maîtresse est disponible
            if (!_masterKeyService.HasKey(userId))
            {
                _logger.LogWarning("Tentative de déchiffrement sans clé maîtresse pour l'utilisateur: {UserId}", userId);
                return BadRequest("Vous devez vous reconnecter pour pouvoir déchiffrer des fichiers privés.");
            }

            try
            {
                // Récupérer la clé maîtresse
                byte[] masterKey = _masterKeyService.GetKey(userId);
                
                // Déchiffrer la clé AES et l'IV avec la clé maîtresse
                byte[] key = CryptoUtils.DecryptWithMasterKey(file.Key, masterKey);
                byte[] iv = CryptoUtils.DecryptWithMasterKey(file.IV, masterKey);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var msDecrypt = new MemoryStream(fileContent))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var msPlain = new MemoryStream())
                    {
                        await csDecrypt.CopyToAsync(msPlain);
                        byte[] decryptedContent = msPlain.ToArray();

                        return File(decryptedContent, "application/octet-stream", file.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du déchiffrement du fichier: {FileName}", file.Name);
                return StatusCode(500, "Erreur interne du serveur lors du déchiffrement du fichier.");
            }
        }

        private async Task<byte[]> GetFileFromLocalIPFSNode(string cid)
        {
            if (_isDevelopment)
            {
                return await GetFileFromLocalIPFSNodeDevelopment(cid);
            }
            else
            {
                return await GetFileFromLocalIPFSNodeProduction(cid);
            }
        }

        private async Task<byte[]> GetFileFromLocalIPFSNodeDevelopment(string cid)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ipfs",
                Arguments = $"cat {cid}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                using (var ms = new MemoryStream())
                {
                    await process.StandardOutput.BaseStream.CopyToAsync(ms);
                    process.WaitForExit();
                    return ms.ToArray();
                }
            }
        }

        private async Task<byte[]> GetFileFromLocalIPFSNodeProduction(string cid)
        {
            var url = $"{_ipfsGatewayUrl}{cid}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsByteArrayAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération depuis IPFS pour le CID: {CID}", cid);
                    throw;
                }
            }
        }

        private class AddResponse
        {
            public string Hash { get; set; }
        }
    }
}
