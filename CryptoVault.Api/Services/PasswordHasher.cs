using System.Security.Cryptography;

namespace CryptoVault.Api.Services;

public interface IPasswordHasher
{
    void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
    bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt);
}

public class PasswordHasher : IPasswordHasher
{
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        // Generar una sal aleatoria criptográficamente segura de 64 bytes
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key; // La clave del HMAC se usa como Salt
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    }

    public bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        if (storedHash.Length != 64 || storedSalt.Length != 128)
            return false;

        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

        // Comparación en tiempo constante para evitar Timing Attacks
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}