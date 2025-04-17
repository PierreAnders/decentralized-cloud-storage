using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using File = TheMerkleTrees.Domain.Models.File;

namespace TheMerkleTrees.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly HttpClient _httpClient;
        private readonly string _ipfsGatewayUrl;
        private readonly string _ipfsApiUrl;
        private readonly bool _isDevelopment;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFileRepository fileRepository, 
            IFolderRepository folderRepository,
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<FilesController> logger)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _httpClient = httpClient;
            _ipfsGatewayUrl = configuration["Ipfs:GatewayUrl"];
            _ipfsApiUrl = configuration["Ipfs:ApiUrl"];
            _isDevelopment = bool.Parse(configuration["Environment:IsDevelopment"]);
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post(File newFile)
        {
            try
            {
                await _fileRepository.CreateAsync(newFile);
                return CreatedAtAction(nameof(Post), new { id = newFile.Id }, newFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du fichier");
                return StatusCode(500, "Une erreur est survenue lors de la création du fichier");
            }
        }
        
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                await _fileRepository.RemoveAsync(name, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier {FileName}", name);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du fichier");
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<List<File>>> GetRootFiles()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            try
            {
                var files = await _fileRepository.GetFilesByUserAsync(userId);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fichiers de l'utilisateur {UserId}", userId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fichiers");
            }
        }

        [HttpGet("folder/{folderId}")]
        public async Task<ActionResult<List<File>>> GetFilesByFolder(string folderId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            try
            {
                // Vérifier que le dossier appartient à l'utilisateur
                var folder = await _folderRepository.GetByIdAsync(folderId);
                if (folder == null || folder.Owner != userId)
                {
                    return NotFound("Dossier non trouvé ou accès non autorisé");
                }
                
                var files = await _fileRepository.GetFilesByFolderAsync(folderId);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fichiers du dossier {FolderId}", folderId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fichiers");
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(1073741824)] // 1 GB
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file, 
            [FromForm] bool isPublic,
            [FromForm] string folderId = null,
            [FromForm] string salt = null,
            [FromForm] string iv = null,
            [FromForm] string encryptedMetadata = null,
            [FromForm] string metadataSalt = null,
            [FromForm] string metadataIV = null)
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

            try
            {
                // Vérifier que le dossier existe et appartient à l'utilisateur
                if (!string.IsNullOrEmpty(folderId))
                {
                    var folder = await _folderRepository.GetByIdAsync(folderId);
                    if (folder == null || folder.Owner != userId)
                    {
                        return BadRequest("Dossier non trouvé ou accès non autorisé");
                    }
                }
                
                byte[] fileContent;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileContent = ms.ToArray();
                }

                // Envoi direct du fichier à IPFS (déjà chiffré par le client ou fichier public)
                var formData = new MultipartFormDataContent();
                formData.Add(new ByteArrayContent(fileContent), "file", file.FileName);
                
                var response = await _httpClient.PostAsync(_ipfsApiUrl, formData);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AddResponse>();
                var cid = result.Hash;
                
                // Création de l'enregistrement du fichier avec le sel, l'IV et les métadonnées chiffrées
                var fileRecord = new File
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    Name = file.FileName,
                    Hash = cid,
                    IsPublic = isPublic,
                    Owner = userId,
                    FolderId = folderId,
                    Salt = salt,
                    IV = iv,
                    Extension = Path.GetExtension(file.FileName),
                    EncryptedMetadata = encryptedMetadata,
                    MetadataSalt = metadataSalt,
                    MetadataIV = metadataIV
                };

                await _fileRepository.CreateAsync(fileRecord);

                return Ok(new { Message = "File uploaded successfully", Cid = cid, Url = $"ipfs://{cid}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du fichier {FileName}", file.FileName);
                return StatusCode(500, "Une erreur est survenue lors de l'upload du fichier");
            }
        }

        [HttpPost("move")]
        public async Task<IActionResult> MoveFile([FromBody] MoveFileRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                // Vérifier que le fichier existe et appartient à l'utilisateur
                var file = await _fileRepository.GetAsync(request.FileName, userId);
                if (file == null)
                {
                    return NotFound("Fichier non trouvé");
                }

                // Vérifier que le dossier de destination existe et appartient à l'utilisateur
                if (!string.IsNullOrEmpty(request.DestinationFolderId))
                {
                    var folder = await _folderRepository.GetByIdAsync(request.DestinationFolderId);
                    if (folder == null || folder.Owner != userId)
                    {
                        return BadRequest("Dossier de destination non trouvé ou accès non autorisé");
                    }
                }

                // Mettre à jour le dossier du fichier
                file.FolderId = request.DestinationFolderId;
                await _fileRepository.UpdateAsync(file);

                return Ok(new { Message = "File moved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du déplacement du fichier {FileName}", request.FileName);
                return StatusCode(500, "Une erreur est survenue lors du déplacement du fichier");
            }
        }

        [HttpGet("file/{name}")]
        public async Task<IActionResult> GetFile(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var file = await _fileRepository.GetAsync(name, userId);
                if (file == null)
                {
                    return NotFound("Fichier non trouvé.");
                }

                byte[] fileContent;
                try
                {
                    fileContent = await GetFileFromLocalIPFSNode(file.Hash);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération via le nœud IPFS : {Message}", ex.Message);
                    return BadRequest("Impossible de récupérer le fichier depuis IPFS.");
                }

                // Retourne le fichier chiffré tel quel, le déchiffrement sera fait côté client
                return File(fileContent, "application/octet-stream", file.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du fichier {FileName}", name);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du fichier");
            }
        }

        [HttpGet("crypto-params/{name}")]
        public async Task<IActionResult> GetCryptoParams(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var file = await _fileRepository.GetAsync(name, userId);
                if (file == null)
                {
                    return NotFound("Fichier non trouvé.");
                }

                // Retourne le sel, l'IV et les métadonnées chiffrées associés au fichier
                return Ok(new { 
                    salt = file.Salt, 
                    iv = file.IV,
                    encryptedMetadata = file.EncryptedMetadata,
                    metadataSalt = file.MetadataSalt,
                    metadataIV = file.MetadataIV
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paramètres cryptographiques pour {FileName}", name);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des paramètres cryptographiques");
            }
        }

        [HttpPut("metadata/{name}")]
        public async Task<IActionResult> UpdateMetadata(
            string name, 
            [FromBody] UpdateMetadataRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var file = await _fileRepository.GetAsync(name, userId);
                if (file == null)
                {
                    return NotFound("Fichier non trouvé.");
                }

                // Mettre à jour les métadonnées chiffrées
                file.EncryptedMetadata = request.EncryptedMetadata;
                file.MetadataSalt = request.MetadataSalt;
                file.MetadataIV = request.MetadataIV;

                await _fileRepository.UpdateAsync(file);

                return Ok(new { Message = "Métadonnées mises à jour avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des métadonnées pour {FileName}", name);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour des métadonnées");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<File>>> SearchFiles([FromQuery] string query, [FromQuery] string folderId = null)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                // Note: Cette recherche ne peut fonctionner que sur les noms de fichiers en clair
                // Les métadonnées chiffrées ne peuvent pas être recherchées côté serveur
                var files = await _fileRepository.SearchFilesByNameAsync(query, userId, folderId);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche de fichiers avec la requête {Query}", query);
                return StatusCode(500, "Une erreur est survenue lors de la recherche de fichiers");
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
                    _logger.LogError(ex, "Erreur lors de la récupération depuis IPFS : {Message}", ex.Message);
                    throw;
                }
            }
        }

        private class AddResponse
        {
            public string Hash { get; set; }
        }

        public class MoveFileRequest
        {
            public string FileName { get; set; }
            public string DestinationFolderId { get; set; }
        }

        public class UpdateMetadataRequest
        {
            public string EncryptedMetadata { get; set; }
            public string MetadataSalt { get; set; }
            public string MetadataIV { get; set; }
        }
    }
}
