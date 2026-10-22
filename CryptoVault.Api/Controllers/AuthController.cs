using CryptoVault.Api.Data;
using CryptoVault.Api.DTOs;
using CryptoVault.Api.Models;
using CryptoVault.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ITotpService _totpService;

    public AuthController(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ITotpService totpService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _totpService = totpService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
        {
            return BadRequest(new { message = "El correo electrónico ya se encuentra registrado." });
        }

        _passwordHasher.CreatePasswordHash(dto.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Email = dto.Email.ToLower(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "USER_REGISTER_SUCCESS",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Detail = $"Usuario {user.Email} registrado exitosamente.",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            role = user.Role,
            token = token,
            isMfaRequired = false,
            message = "Usuario registrado exitosamente en la Bóveda."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !_passwordHasher.VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user?.Id,
                Action = "LOGIN_FAILED",
                IpAddress = ipAddress,
                Detail = $"Intento fallido de login para la cuenta: {dto.Email}",
                IsSuccess = false,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        user.LastLoginAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "LOGIN_SUCCESS",
            IpAddress = ipAddress,
            Detail = $"Inicio de sesión exitoso para {user.Email}",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            role = user.Role,
            token = token,
            isMfaRequired = user.IsMfaEnabled,
            message = "Inicio de sesión exitoso."
        });
    }

    [HttpPost("mfa/setup")]
    public async Task<IActionResult> SetupMfa([FromBody] MfaSetupDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        // Generar Secreto TOTP e imagen del Código QR
        var secret = _totpService.GenerateSecretKey();
        var qrCodeImage = _totpService.GenerateQrCodeImage(user.Email, secret);

        user.TotpSecret = secret;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            secret = secret,
            qrCodeUrl = qrCodeImage,
            message = "Escanee la imagen QR en Google Authenticator o Microsoft Authenticator."
        });
    }

    [HttpPost("mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null || string.IsNullOrEmpty(user.TotpSecret))
        {
            return BadRequest(new { message = "El usuario no ha iniciado la configuración de MFA." });
        }

        bool isValid = _totpService.VerifyCode(user.TotpSecret, dto.Code);
        if (!isValid)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "MFA_VERIFY_FAILED",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Detail = "Código TOTP inválido o expirado.",
                IsSuccess = false,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return BadRequest(new { message = "Código de verificación inválido o expirado." });
        }

        user.IsMfaEnabled = true;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "MFA_ENABLED_SUCCESS",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Detail = "MFA/TOTP activado exitosamente para la cuenta.",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { message = "Autenticación Multifactor (MFA) activada con éxito." });
    }
}