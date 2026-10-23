using System.Security.Claims;
using CryptoVault.Api.Data;
using CryptoVault.Api.DTOs;
using CryptoVault.Api.Models;
using CryptoVault.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Exige autenticación con Token JWT para todas las rutas
public class VaultController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;

    // Clave maestra simulada por usuario (En un HSM o KMS de producción se derivaría por usuario)
    private static readonly byte[] MasterKey = new byte[32]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
        0x17, 0x18, 0x19, 0x20, 0x21, 0x22, 0x23, 0x24,
        0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32
    };

    public VaultController(AppDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateVaultItemDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized(new { message = "Token de usuario no válido." });

        // Cifrar la contraseña en claro usando AES-256-GCM
        var (encryptedData, iv, authTag) = _encryptionService.Encrypt(dto.Password, MasterKey);

        var vaultItem = new VaultItem
        {
            UserId = userId,
            Title = dto.Title,
            Username = dto.Username,
            EncryptedData = encryptedData,
            Iv = iv,
            AuthTag = authTag,
            CreatedAt = DateTime.UtcNow
        };

        _context.VaultItems.Add(vaultItem);

        // Registro de Auditoría
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "VAULT_ITEM_CREATED",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Detail = $"Elemento de bóveda '{vaultItem.Title}' creado y cifrado con AES-256-GCM.",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new VaultItemResponseDto
        {
            Id = vaultItem.Id,
            Title = vaultItem.Title,
            Username = vaultItem.Username,
            CreatedAt = vaultItem.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyItems()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        var items = await _context.VaultItems
            .Where(v => v.UserId == userId)
            .Select(v => new VaultItemResponseDto
            {
                Id = v.Id,
                Title = v.Title,
                Username = v.Username,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}/decrypt")]
    public async Task<IActionResult> DecryptItem(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        var vaultItem = await _context.VaultItems.FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
        if (vaultItem == null)
            return NotFound(new { message = "Elemento no encontrado en su bóveda." });

        try
        {
            // Descifrar y validar integridad con el AuthTag
            string plainPassword = _encryptionService.Decrypt(vaultItem.EncryptedData, vaultItem.Iv, vaultItem.AuthTag, MasterKey);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "VAULT_ITEM_DECRYPTED",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Detail = $"Elemento '{vaultItem.Title}' descifrado exitosamente por el propietario.",
                IsSuccess = true,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = vaultItem.Id,
                title = vaultItem.Title,
                username = vaultItem.Username,
                decryptedPassword = plainPassword
            });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Error al descifrar el elemento. Los datos o el tag de autenticación han sido alterados." });
        }
    }
}