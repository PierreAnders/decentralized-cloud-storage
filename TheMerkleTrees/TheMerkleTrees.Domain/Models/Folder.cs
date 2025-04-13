namespace TheMerkleTrees.Domain.Models
{
    public class Folder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? ParentId { get; set; } // null pour les dossiers racine
        public string? Owner { get; set; }
        public bool IsPublic { get; set; }
    }
}
