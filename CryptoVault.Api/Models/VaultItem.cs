namespace CryptoVault.Api.Models;

public class VaultItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Relación con el Usuario propietario
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;

    // Cifrado Authenticated Encryption (AES-256-GCM)
    public string EncryptedData { get; set; } = string.Empty; // Texto cifrado en Base64
    public string Iv { get; set; } = string.Empty;            // Vector de Inicialización
    public string AuthTag { get; set; } = string.Empty;       // Tag de Autenticación de Integridad

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}