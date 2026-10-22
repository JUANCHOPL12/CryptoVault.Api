using System.ComponentModel.DataAnnotations;

namespace CryptoVault.Api.DTOs;

public class ChangeRoleDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string NewRole { get; set; } = string.Empty; // "Admin", "User", "Auditor"
}