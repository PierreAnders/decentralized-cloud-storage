using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TheMerkleTrees.Infrastructure.Entities;

public class FolderEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ParentId { get; set; } // null pour les dossiers racine
    public string? Owner { get; set; }
    public bool IsPublic { get; set; }
}