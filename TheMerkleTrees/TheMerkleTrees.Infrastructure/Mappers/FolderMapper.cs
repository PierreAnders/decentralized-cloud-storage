using TheMerkleTrees.Domain.Models;
using TheMerkleTrees.Infrastructure.Entities;

namespace TheMerkleTrees.Infrastructure.Mappers;

public static  class FolderMapper
{
    public static Folder ToDomain(this FolderEntity entity)
    {
        return new Folder
        {
            Id = entity.Id,
            Name = entity.Name,
            ParentId = entity.ParentId,
            Owner = entity.Owner,
            IsPublic = entity.IsPublic,
        };
    }

    public static FolderEntity ToEntity(this Folder folder)
    {
        return new FolderEntity
        {
            Id = folder.Id,
            Name = folder.Name,
            ParentId = folder.ParentId,
            Owner = folder.Owner,
            IsPublic = folder.IsPublic,
        };
    }
}