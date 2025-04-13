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

        [HttpGet("folder/{folderId}")]
        public async Task<ActionResult<List<File>>> GetFilesByFolder(string folderId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }
            
            // Vérifier que le dossier appartient à l'utilisateur
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null || folder.Owner != userId)
            {
                return NotFound("Dossier non trouvé ou accès non autorisé");
            }
            
            var files = await _fileRepository.GetFilesByFolderAsync(folderId);

            return files;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(1073741824)] 
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file, 
            [FromForm] string category,
            [FromForm] bool isPublic, 
            [FromForm] string userAddress, 
            [FromForm] string folderId = null,
            [FromForm] string salt = null,
            [FromForm] string iv = null)
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
            var url = $"ipfs://{cid}";
            
            // Création de l'enregistrement du fichier avec le sel et l'IV
            var fileRecord = new File
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = file.FileName,
                Hash = cid,
                Category = category,
                IsPublic = isPublic,
                Owner = userId,
                FolderId = folderId, // Nouveau champ pour le dossier
                Salt = salt,
                IV = iv, // Nouveau champ pour l'IV
                Extension = Path.GetExtension(file.FileName)
            };

            await _fileRepository.CreateAsync(fileRecord);

            return Ok(new { Message = "File uploaded successfully", Url = url });
        }

        [HttpPost("move")]
        public async Task<IActionResult> MoveFile([FromBody] MoveFileRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

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

        // Endpoint pour récupérer le fichier chiffré directement
        [HttpGet("file/{name}")]
        public async Task<IActionResult> GetFile(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            var decodedName = Uri.UnescapeDataString(name);
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
                _logger.LogError(ex, "Erreur lors de la récupération via le nœud IPFS : {Message}", ex.Message);
                return BadRequest("Impossible de récupérer le fichier depuis IPFS.");
            }

            // Retourne le fichier chiffré tel quel, le déchiffrement sera fait côté client
            return File(fileContent, "application/octet-stream", file.Name);
        }

        // Endpoint pour récupérer le sel et l'IV associés à un fichier
        [HttpGet("crypto-params/{name}")]
        public async Task<IActionResult> GetCryptoParams(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            var file = await _fileRepository.GetAsync(name, userId);
            if (file == null)
            {
                return NotFound("Fichier non trouvé.");
            }

            // Retourne le sel et l'IV associés au fichier
            return Ok(new { salt = file.Salt, iv = file.IV });
        }

        // Maintien de l'ancien endpoint pour la compatibilité
        [HttpGet("salt/{name}")]
        public async Task<IActionResult> GetSalt(string name)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            var file = await _fileRepository.GetAsync(name, userId);
            if (file == null)
            {
                return NotFound("Fichier non trouvé.");
            }

            // Retourne le sel associé au fichier
            return Ok(new { salt = file.Salt });
        }

        // Maintien de l'ancien endpoint pour la compatibilité
        [HttpGet("decrypt/{name}")]
        public async Task<IActionResult> DecryptFile(string name)
        {
            // Redirection vers le nouvel endpoint
            return await GetFile(name);
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
    }
}
