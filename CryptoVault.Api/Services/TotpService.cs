using OtpNet;
using QRCoder;

namespace CryptoVault.Api.Services;

public interface ITotpService
{
    string GenerateSecretKey();
    string GenerateQrCodeImage(string email, string secretKey);
    bool VerifyCode(string secretKey, string code);
}

public class TotpService : ITotpService
{
    public string GenerateSecretKey()
    {
        // Generar clave secreta aleatoria Base32 de 160 bits (20 bytes)
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeImage(string email, string secretKey)
    {
        // Construir el URI estándar OTPAUTH para Authenticator
        string issuer = "CryptoVault";
        string otpAuthUrl = $"otpauth://totp/{issuer}:{email}?secret={secretKey}&issuer={issuer}&digits=6";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);

        // Retornar en formato Data URI Base64 listo para etiquetas <img> en React
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }

    public bool VerifyCode(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
            return false;

        var bytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(bytes);

        // Validar el código de 6 dígitos dentro de la ventana de tiempo de +/- 1 periodo (30s)
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }
}