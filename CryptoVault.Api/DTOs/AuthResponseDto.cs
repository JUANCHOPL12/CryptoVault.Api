namespace CryptoVault.Api.DTOs;

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsMfaRequired { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}