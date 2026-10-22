namespace CryptoVault.Api.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Relación con el Usuario que ejecuta la acción
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;       // Ej: "LOGIN_SUCCESS", "VAULT_ACCESS"
    public string IpAddress { get; set; } = string.Empty;    // IP de origen
    public string Detail { get; set; } = string.Empty;       // Detalle del evento
    public bool IsSuccess { get; set; } = true;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}