namespace CryptoVault.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;

    // Almacenamiento seguro de contraseñas (Hash + Salt)
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    // Control de Acceso Basado en Roles (RBAC)
    public string Role { get; set; } = "User"; // "Admin", "User", "Auditor"

    // Autenticación Multifactor (MFA / TOTP)
    public string? TotpSecret { get; set; }
    public bool IsMfaEnabled { get; set; } = false;

    // Métricas de Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Relaciones de Navegación (Ahora reconocidas correctamente)
    public ICollection<VaultItem> VaultItems { get; set; } = new List<VaultItem>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}