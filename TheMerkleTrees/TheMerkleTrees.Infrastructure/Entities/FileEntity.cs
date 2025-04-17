using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TheMerkleTrees.Infrastructure.Entities;

public class FileEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
 public string Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public bool IsPublic { get; set; }
        public string Owner { get; set; }
        public string Extension { get; set; }
        public string Salt { get; set; }
        public string IV { get; set; }
        public string FolderId { get; set; }
        public string EncryptedMetadata { get; set; }
        public string MetadataSalt { get; set; }
        public string MetadataIV { get; set; }
}