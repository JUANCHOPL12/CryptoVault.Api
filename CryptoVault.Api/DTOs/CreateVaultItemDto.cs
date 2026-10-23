using System.ComponentModel.DataAnnotations;

namespace CryptoVault.Api.DTOs;

public class CreateVaultItemDto
{
    [Required]
    public string Title { get; set; } = string.Empty; // Ej: "Correo Institucional"

    [Required]
    public string Username { get; set; } = string.Empty; // Ej: "estudiante@cun.edu.co"

    [Required]
    public string Password { get; set; } = string.Empty; // La contraseña en claro que será cifrada
}