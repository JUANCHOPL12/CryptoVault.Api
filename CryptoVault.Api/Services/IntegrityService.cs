using System.Security.Cryptography;
using System.Text;

namespace CryptoVault.Api.Services;

public interface IIntegrityService
{
    string ComputeSha256Hash(byte[] inputBytes);
    (string PublicKeyB64, ECDiffieHellmanCng EcdhInstance) GenerateEcdhKeyPair();
    byte[] DeriveSharedSecret(ECDiffieHellmanCng myEcdh, string otherPublicKeyB64);
}

public class IntegrityService : IIntegrityService
{
    // 1. Cálculo de Hash de Integridad SHA-256
    public string ComputeSha256Hash(byte[] inputBytes)
    {
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower(); // Retorna la huella en formato Hexadecimal
    }

    // 2. Generación de Par de Claves ECDH (Curva NIST P-256)
    public (string PublicKeyB64, ECDiffieHellmanCng EcdhInstance) GenerateEcdhKeyPair()
    {
        var ecdh = new ECDiffieHellmanCng(256);
        ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
        ecdh.HashAlgorithm = CngAlgorithm.Sha256;

        byte[] publicKeyBytes = ecdh.PublicKey.ToByteArray();
        return (Convert.ToBase64String(publicKeyBytes), ecdh);
    }

    // 3. Derivación del Secreto Compartido usando la Clave Pública del otro extremo
    public byte[] DeriveSharedSecret(ECDiffieHellmanCng myEcdh, string otherPublicKeyB64)
    {
        byte[] otherPublicKeyBytes = Convert.FromBase64String(otherPublicKeyB64);
        using var otherPublicKey = CngKey.Import(otherPublicKeyBytes, CngKeyBlobFormat.EccPublicBlob);

        return myEcdh.DeriveKeyMaterial(otherPublicKey);
    }
}