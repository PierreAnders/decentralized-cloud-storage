namespace TheMerkleTrees.Domain.Models
{
    public class File
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public string Category { get; set; } // Maintenu pour la compatibilité avec l'existant
        public bool IsPublic { get; set; }
        public string Owner { get; set; }
        public string Extension { get; set; }
        public string Salt { get; set; }
        
        // Nouveaux champs ajoutés
        public string FolderId { get; set; } // Référence au dossier contenant le fichier
        public string IV { get; set; } // Vecteur d'initialisation pour le chiffrement AES-GCM
    }
}