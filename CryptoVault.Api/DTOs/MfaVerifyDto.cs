using System.ComponentModel.DataAnnotations;

namespace CryptoVault.Api.DTOs;

public class MfaVerifyDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código MFA debe tener exactamente 6 dígitos.")]
    public string Code { get; set; } = string.Empty;
}