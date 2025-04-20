using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TheMerkleTrees.Domain.Interfaces.Repositories;
using TheMerkleTrees.Infrastructure.Configurations;
using TheMerkleTrees.Domain.Models;
using TheMerkleTrees.Infrastructure.Entities;
using TheMerkleTrees.Infrastructure.Mappers;
using File = TheMerkleTrees.Domain.Models.File;

namespace TheMerkleTrees.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly IMongoCollection<FileEntity> _filesCollection;

    public FileRepository(IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoClient = new MongoClient(
            mongoDBSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            mongoDBSettings.Value.DatabaseName);

        _filesCollection = mongoDatabase.GetCollection<FileEntity>("Files");
    }

    public async Task<List<File>> GetAsync() =>
        (await _filesCollection.Find(_ => true).ToListAsync())
        .Select(entity => entity.ToDomain())
        .ToList();

    public async Task<File?> GetAsync(string name, string userId) =>
        (await _filesCollection.Find(file => file.Name == name && file.Owner == userId).FirstOrDefaultAsync())?.ToDomain();

    public async Task<File?> GetByIdAsync(string id) =>
        (await _filesCollection.Find(file => file.Id == id).FirstOrDefaultAsync())?.ToDomain();

    public async Task<List<File>> GetFilesByUserAsync(string userId) =>
        (await _filesCollection.Find(file => file.Owner == userId).ToListAsync())
        .Select(entity => entity.ToDomain())
        .ToList();

    public async Task<List<File>> GetFilesByFolderAsync(string folderId) =>
        (await _filesCollection.Find(file => file.FolderId == folderId).ToListAsync())
        .Select(entity => entity.ToDomain())
        .ToList();

    public async Task<List<File>> SearchFilesByNameAsync(string query, string userId, string folderId = null)
    {
        var builder = Builders<FileEntity>.Filter;
        var filter = builder.And(
            builder.Eq(file => file.Owner, userId),
            builder.Regex(file => file.Name, new MongoDB.Bson.BsonRegularExpression(query, "i"))
        );

        if (!string.IsNullOrEmpty(folderId))
        {
            filter = builder.And(filter, builder.Eq(file => file.FolderId, folderId));
        }

        return (await _filesCollection.Find(filter).ToListAsync())
            .Select(entity => entity.ToDomain())
            .ToList();
    }

    public async Task CreateAsync(File newFile) =>
        await _filesCollection.InsertOneAsync(newFile.ToEntity());

    public async Task UpdateAsync(string id, File updateFile) =>
        await _filesCollection.ReplaceOneAsync(x => x.Id == id, updateFile.ToEntity());

    public async Task UpdateAsync(File updatedFile) =>
        await _filesCollection.ReplaceOneAsync(file => file.Id == updatedFile.Id, updatedFile.ToEntity());

    public async Task RemoveAsync(string name, string userId) =>
        await _filesCollection.DeleteOneAsync(x => x.Name == name && x.Owner == userId);

    public async Task RemoveAsync(string id) =>
        await _filesCollection.DeleteOneAsync(x => x.Id == id);
    
    public async Task<int> CountFilesByHashAsync(string hash, string excludeUserId)
    {
        var filter = Builders<FileEntity>.Filter.And(
            Builders<FileEntity>.Filter.Eq(f => f.Hash, hash),
            Builders<FileEntity>.Filter.Ne(f => f.Owner, excludeUserId)
        );
        return (int)await _filesCollection.CountDocumentsAsync(filter);
    }
}