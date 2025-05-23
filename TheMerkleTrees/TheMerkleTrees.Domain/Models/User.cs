namespace TheMerkleTrees.Domain.Models;

public class User
{
    public string Id { get; set; }
    
    public string Email { get; set; }
    
    public string PasswordHash { get; set; }
    public string EncryptionKey { get; set; }
}