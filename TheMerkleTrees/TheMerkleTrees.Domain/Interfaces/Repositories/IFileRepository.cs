﻿using TheMerkleTrees.Domain.Models;
using File = TheMerkleTrees.Domain.Models.File;

namespace TheMerkleTrees.Domain.Interfaces.Repositories
{
    public interface IFileRepository
    {   
        Task<List<File>> GetAsync();
        Task<File?> GetAsync(string name, string userId);
        Task<File?> GetByIdAsync(string id);
        Task<List<File>> GetFilesByUserAsync(string userId);
        Task<List<File>> GetFilesByFolderAsync(string folderId);
        Task CreateAsync(File newFile);
        Task UpdateAsync(File updatedFile);
        Task RemoveAsync(string name, string userId);
        Task RemoveAsync(string id);
        Task<List<File>> SearchFilesByNameAsync(string query, string userId, string folderId = null);
        Task<int> CountFilesByHashAsync(string hash, string excludeUserId);
    }
}