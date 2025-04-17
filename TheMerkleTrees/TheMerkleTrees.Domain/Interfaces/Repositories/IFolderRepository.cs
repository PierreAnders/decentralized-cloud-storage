namespace TheMerkleTrees.Domain.Interfaces.Repositories
{
    public interface IFolderRepository
    {
        Task<List<Models.Folder>> GetByUserAsync(string userId);
        Task<List<Models.Folder>> GetRootFoldersByUserAsync(string userId);
        Task<List<Models.Folder>> GetSubfoldersByParentAsync(string parentId);
        Task<Models.Folder> GetByIdAsync(string id);
        Task CreateAsync(Models.Folder folder);
        Task UpdateAsync(Models.Folder folder);
        Task RemoveAsync(string id);
    }
}