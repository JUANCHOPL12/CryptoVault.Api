using CryptoVault.Api.Data;
using CryptoVault.Api.Services;
using Microsoft.EntityFrameworkCore;

// 1. Inicialización del WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);

// 2. Configurar la conexión a la base de datos SQL Server Express
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();

// 2. Registrar los Servicios Criptográficos e IAM
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITotpService, TotpService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); var app = builder.Build();

// 5. Configuración del Pipeline de peticiones HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();