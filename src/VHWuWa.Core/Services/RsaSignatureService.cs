using System.Security.Cryptography;
using System.Text;

namespace VHWuWa.Core.Services;

/// <summary>Ký &amp; xác minh chữ ký số. Dùng RSA (built-in .NET) — public key nhúng trong app,
/// private key giữ ngoài repo / GitHub Secrets.</summary>
public interface ISignatureService
{
    /// <summary>Tạo cặp khóa mới, trả (publicKeyPem, privateKeyPem).</summary>
    (string publicPem, string privatePem) GenerateKeyPair(int keySizeBits = 3072);
    /// <summary>Ký dữ liệu bằng private key PEM, trả chữ ký base64.</summary>
    string SignBase64(byte[] data, string privateKeyPem);
    /// <summary>Xác minh chữ ký base64 với public key PEM.</summary>
    bool Verify(byte[] data, string signatureBase64, string publicKeyPem);
}

public sealed class RsaSignatureService : ISignatureService
{
    private static readonly HashAlgorithmName Alg = HashAlgorithmName.SHA256;

    public (string publicPem, string privatePem) GenerateKeyPair(int keySizeBits = 3072)
    {
        using var rsa = RSA.Create(keySizeBits);
        return (rsa.ExportSubjectPublicKeyInfoPem(), rsa.ExportPkcs8PrivateKeyPem());
    }

    public string SignBase64(byte[] data, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var sig = rsa.SignData(data, Alg, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(sig);
    }

    public bool Verify(byte[] data, string signatureBase64, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(signatureBase64) || string.IsNullOrWhiteSpace(publicKeyPem))
            return false;
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            var sig = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(data, sig, Alg, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);
}
