using MongoDB.Driver;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using TheMerkleTrees.Domain.Models;
using TheMerkleTrees.Infrastructure.Entities;
using TheMerkleTrees.Infrastructure.Mappers;

namespace TheMerkleTrees.Infrastructure.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly IMongoCollection<FolderEntity> _foldersCollection;

        public FolderRepository(IMongoDatabase database)
        {
            _foldersCollection = database.GetCollection<FolderEntity>("folders");
        }

        public async Task<List<Folder>> GetByUserAsync(string userId)
        {
            var entities = await _foldersCollection.Find(folder => folder.Owner == userId).ToListAsync();
            return entities.Select(entity => entity.ToDomain()).ToList();
        }

        public async Task<List<Folder>> GetRootFoldersByUserAsync(string userId)
        {
            var entities = await _foldersCollection.Find(folder => folder.Owner == userId && (folder.ParentId == null || folder.ParentId == "")).ToListAsync();
            return entities.Select(entity => entity.ToDomain()).ToList();
        }

        public async Task<List<Folder>> GetSubfoldersByParentAsync(string parentId)
        {
            var entities = await _foldersCollection.Find(folder => folder.ParentId == parentId).ToListAsync();
            return entities.Select(entity => entity.ToDomain()).ToList();
        }

        public async Task<Folder> GetByIdAsync(string id)
        {
            var entity = await _foldersCollection.Find(folder => folder.Id == id).FirstOrDefaultAsync();
            return entity?.ToDomain();
        }

        public async Task CreateAsync(Folder folder)
        {
            var entity = folder.ToEntity();
            await _foldersCollection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(Folder folder)
        {
            var entity = folder.ToEntity();
            await _foldersCollection.ReplaceOneAsync(f => f.Id == folder.Id, entity);
        }

        public async Task RemoveAsync(string id)
        {
            await _foldersCollection.DeleteOneAsync(folder => folder.Id == id);
        }
    }
}
