using System.Security.Cryptography;
using System.Text;

namespace CryptoVault.Api.Services;

public interface IEncryptionService
{
    (string EncryptedData, string Iv, string AuthTag) Encrypt(string plainText, byte[] key);
    string Decrypt(string encryptedDataB64, string ivB64, string authTagB64, byte[] key);
    byte[] GenerateMasterKey();
}

public class EncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96 bits para el IV de AES-GCM
    private const int TagSize = 16;   // 128 bits para el Tag de Integridad

    // Generar una Clave Maestra de 256 bits (32 bytes)
    public byte[] GenerateMasterKey()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    // Cifrar con AES-256-GCM
    public (string EncryptedData, string Iv, string AuthTag) Encrypt(string plainText, byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("La clave debe ser de 256 bits (32 bytes).");

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = new byte[NonceSize];
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        // Llenar el IV con bytes aleatorios criptográficamente seguros
        RandomNumberGenerator.Fill(nonce);

        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        return (
            Convert.ToBase64String(cipherBytes),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag)
        );
    }

    // Descifrar con AES-256-GCM y validar Integridad
    public string Decrypt(string encryptedDataB64, string ivB64, string authTagB64, byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("La clave debe ser de 256 bits (32 bytes).");

        byte[] cipherBytes = Convert.FromBase64String(encryptedDataB64);
        byte[] nonce = Convert.FromBase64String(ivB64);
        byte[] tag = Convert.FromBase64String(authTagB64);
        byte[] plainBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(key, TagSize))
        {
            // Intenta descifrar. Si el Tag no coincide, arroja CryptographicException
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}