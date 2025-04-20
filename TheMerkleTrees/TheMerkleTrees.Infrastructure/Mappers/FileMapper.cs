using TheMerkleTrees.Infrastructure.Entities;
using File = TheMerkleTrees.Domain.Models.File;

namespace TheMerkleTrees.Infrastructure.Mappers;

public static class FileMapper
{
    public static File ToDomain (this FileEntity entity)
    {
        return new File
        {
            Id = entity.Id,
            Hash = entity.Hash,
            Name = entity.Name,
            IsPublic = entity.IsPublic,
            Owner = entity.Owner,
            IV = entity.IV,
            Extension = entity.Extension,
            Salt = entity.Salt,
            FolderId = entity.FolderId,
            EncryptedMetadata = entity.EncryptedMetadata,
            MetadataSalt = entity.MetadataSalt,
            MetadataIV = entity.MetadataIV
        };
    }

    public static FileEntity ToEntity(this File file)
    {
        return new FileEntity
        {
            Id = file.Id,
            Hash = file.Hash,
            Name = file.Name,
            IsPublic = file.IsPublic,
            Owner = file.Owner,
            IV = file.IV,
            Extension = file.Extension,
            Salt = file.Salt,
            FolderId = file.FolderId,
            EncryptedMetadata = file.EncryptedMetadata,
            MetadataSalt = file.MetadataSalt,
            MetadataIV = file.MetadataIV
        };
    }
}