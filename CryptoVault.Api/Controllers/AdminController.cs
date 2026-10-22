using CryptoVault.Api.Data;
using CryptoVault.Api.DTOs;
using CryptoVault.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // Solo accesible por usuarios con Rol Admin
    [HttpGet("users")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.IsMfaEnabled,
                u.CreatedAt,
                u.LastLoginAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // Promoción de Roles: Solo accesible por Administradores
    [HttpPost("change-role")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado." });

        string oldRole = user.Role;
        user.Role = dto.NewRole;

        // Registrar auditoría del cambio de rol
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "ROLE_CHANGED",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Detail = $"Rol cambiado de '{oldRole}' a '{dto.NewRole}' por un Administrador.",
            IsSuccess = true,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { message = $"El rol del usuario {user.Email} fue actualizado exitosamente a '{dto.NewRole}'." });
    }

    // Registro de Auditoría Global: Accesible por Admin o Auditor
    [HttpGet("audit-logs")]
    [Authorize(Policy = "RequireAuditorRole")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .ToListAsync();

        return Ok(logs);
    }
}