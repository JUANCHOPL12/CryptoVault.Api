using System.ComponentModel.DataAnnotations;

namespace CryptoVault.Api.DTOs;

public class MfaSetupDto
{
    [Required]
    public Guid UserId { get; set; }
}