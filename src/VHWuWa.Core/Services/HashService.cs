using System.Security.Cryptography;

namespace VHWuWa.Core.Services;

/// <summary>Tính SHA-256 cho file, stream, byte[].</summary>
public interface IHashService
{
    string Sha256File(string path);
    string Sha256Bytes(byte[] data);
    Task<string> Sha256StreamAsync(Stream stream, CancellationToken ct = default);
    bool Verify(string path, string expectedHex);
}

public sealed class HashService : IHashService
{
    public string Sha256File(string path)
    {
        using var fs = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
    }

    public string Sha256Bytes(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public async Task<string> Sha256StreamAsync(Stream stream, CancellationToken ct = default)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool Verify(string path, string expectedHex)
    {
        if (string.IsNullOrWhiteSpace(expectedHex)) return true; // không có hash yêu cầu -> bỏ qua
        return string.Equals(Sha256File(path), expectedHex.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
