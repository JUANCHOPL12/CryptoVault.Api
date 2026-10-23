using System.ComponentModel.DataAnnotations;

namespace CryptoVault.Api.Models;

public class VaultItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Relación con el Usuario propietario
    [Required]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    // Campo de usuario del servicio (ej. correo o usuario guardado)
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    // Cifrado Authenticated Encryption (AES-256-GCM)
    [Required]
    public string EncryptedData { get; set; } = string.Empty; // Texto cifrado en Base64

    [Required]
    public string Iv { get; set; } = string.Empty;            // Vector de Inicialización

    [Required]
    public string AuthTag { get; set; } = string.Empty;       // Tag de Autenticación de Integridad

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}