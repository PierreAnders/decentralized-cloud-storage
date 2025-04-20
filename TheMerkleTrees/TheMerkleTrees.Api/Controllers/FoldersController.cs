using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using TheMerkleTrees.Domain.Models;

namespace TheMerkleTrees.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<FoldersController> _logger;

        public FoldersController(
            IFolderRepository folderRepository,
            IFileRepository fileRepository,
            ILogger<FoldersController> logger)
        {
            _folderRepository = folderRepository;
            _fileRepository = fileRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Folder>>> GetUserFolders()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var folders = await _folderRepository.GetByUserAsync(userId);
            return folders;
        }

        [HttpGet("root")]
        public async Task<ActionResult<List<Folder>>> GetRootFolders()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var folders = await _folderRepository.GetRootFoldersByUserAsync(userId);
            return folders;
        }

        [HttpGet("{id}/subfolders")]
        public async Task<ActionResult<List<Folder>>> GetSubfolders(string id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Vérifier que le dossier parent appartient à l'utilisateur
            var parentFolder = await _folderRepository.GetByIdAsync(id);
            if (parentFolder == null || parentFolder.Owner != userId)
            {
                return NotFound("Dossier parent non trouvé ou accès non autorisé");
            }

            var subfolders = await _folderRepository.GetSubfoldersByParentAsync(id);
            return subfolders;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Folder>> GetFolder(string id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var folder = await _folderRepository.GetByIdAsync(id);
            if (folder == null || folder.Owner != userId)
            {
                return NotFound("Dossier non trouvé ou accès non autorisé");
            }

            return folder;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] FolderRequest newFolder)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Vérifier que le dossier parent existe et appartient à l'utilisateur
            if (!string.IsNullOrEmpty(newFolder.ParentId))
            {
                var parentFolder = await _folderRepository.GetByIdAsync(newFolder.ParentId);
                if (parentFolder == null || parentFolder.Owner != userId)
                {
                    return BadRequest("Dossier parent non trouvé ou accès non autorisé");
                }
            }

            var folder = new Folder();

            // Définir le propriétaire et les dates
            folder.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            folder.Owner = userId;
            folder.Name = newFolder.Name;
            folder.ParentId = newFolder.ParentId;
            folder.IsPublic = newFolder.IsPublic;
            folder.CreatedAt = DateTime.UtcNow;
            folder.UpdatedAt = DateTime.UtcNow;

            await _folderRepository.CreateAsync(folder);

            return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
        }
        
        public class FolderRequest
        {
            public string Name { get; set; }
            public string? ParentId { get; set; } // null pour les dossiers racine
            public bool IsPublic { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(string id, [FromBody] FolderUpdateRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var folder = await _folderRepository.GetByIdAsync(id);
            if (folder == null || folder.Owner != userId)
            {
                return NotFound("Dossier non trouvé ou accès non autorisé");
            }

            // Vérifier que le nouveau dossier parent existe et appartient à l'utilisateur
            if (!string.IsNullOrEmpty(request.ParentId) && request.ParentId != folder.ParentId)
            {
                // Vérifier que le nouveau parent n'est pas un descendant du dossier actuel
                if (await IsDescendant(request.ParentId, id))
                {
                    return BadRequest("Opération impossible : le dossier parent ne peut pas être un descendant du dossier actuel");
                }

                var parentFolder = await _folderRepository.GetByIdAsync(request.ParentId);
                if (parentFolder == null || parentFolder.Owner != userId)
                {
                    return BadRequest("Dossier parent non trouvé ou accès non autorisé");
                }
            }

            // Mettre à jour les propriétés du dossier
            folder.Name = request.Name ?? folder.Name;
            folder.ParentId = request.ParentId;
            folder.IsPublic = request.IsPublic ?? folder.IsPublic;
            folder.UpdatedAt = DateTime.UtcNow;

            await _folderRepository.UpdateAsync(folder);

            return Ok(folder);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(string id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var folder = await _folderRepository.GetByIdAsync(id);
            if (folder == null || folder.Owner != userId)
            {
                return NotFound("Dossier non trouvé ou accès non autorisé");
            }

            // Récupérer tous les sous-dossiers récursivement
            var allSubfolders = await GetAllDescendantFolders(id);
            var allFolderIds = new List<string> { id };
            allFolderIds.AddRange(allSubfolders.Select(f => f.Id));

            // Supprimer tous les fichiers dans ces dossiers
            foreach (var folderId in allFolderIds)
            {
                var files = await _fileRepository.GetFilesByFolderAsync(folderId);
                foreach (var file in files)
                {
                    await _fileRepository.RemoveAsync(file.Name, userId);
                }
            }

            // Supprimer tous les sous-dossiers
            foreach (var subfolderId in allFolderIds.Skip(1))
            {
                await _folderRepository.RemoveAsync(subfolderId);
            }

            // Supprimer le dossier principal
            await _folderRepository.RemoveAsync(id);

            return NoContent();
        }

        private async Task<bool> IsDescendant(string potentialDescendantId, string ancestorId)
        {
            if (string.IsNullOrEmpty(potentialDescendantId))
                return false;

            if (potentialDescendantId == ancestorId)
                return true;

            var folder = await _folderRepository.GetByIdAsync(potentialDescendantId);
            if (folder == null || string.IsNullOrEmpty(folder.ParentId))
                return false;

            return await IsDescendant(folder.ParentId, ancestorId);
        }

        private async Task<List<Folder>> GetAllDescendantFolders(string folderId)
        {
            var result = new List<Folder>();
            var subfolders = await _folderRepository.GetSubfoldersByParentAsync(folderId);
            
            result.AddRange(subfolders);
            
            foreach (var subfolder in subfolders)
            {
                var descendants = await GetAllDescendantFolders(subfolder.Id);
                result.AddRange(descendants);
            }
            
            return result;
        }

        public class FolderUpdateRequest
        {
            public string Name { get; set; }
            public string ParentId { get; set; }
            public bool? IsPublic { get; set; }
        }
    }
}
