using System.IO.Compression;
using System.Text;
using VHWuWa.Core.Models;

namespace VHWuWa.Core.Services;

/// <summary>Đọc &amp; kiểm tra gói .vhwpack. .vhwpack là ZIP gồm: manifest.json, payload/, signature.sig.</summary>
public sealed class VhwPackageReader : IDisposable
{
    public const string ManifestEntry = "manifest.json";
    public const string SignatureEntry = "signature.sig";
    public const long MaxSingleFileBytes = 2L * 1024 * 1024 * 1024;   // 2 GB / file
    public const long MaxTotalBytes = 8L * 1024 * 1024 * 1024;        // 8 GB / gói
    public const int MaxFileCount = 20000;

    private readonly ZipArchive _zip;

    public PackageManifest Manifest { get; }
    public byte[] ManifestBytes { get; }
    public string Signature { get; }

    private VhwPackageReader(ZipArchive zip, PackageManifest manifest, byte[] manifestBytes, string signature)
    {
        _zip = zip;
        Manifest = manifest;
        ManifestBytes = manifestBytes;
        Signature = signature;
    }

    public static VhwPackageReader Open(string path)
    {
        var zip = ZipFile.OpenRead(path);
        var m = zip.GetEntry(ManifestEntry)
            ?? throw new InvalidDataException("Gói thiếu manifest.json.");
        byte[] bytes;
        using (var s = m.Open())
        using (var ms = new MemoryStream())
        {
            s.CopyTo(ms);
            bytes = ms.ToArray();
        }
        var manifest = VhwJson.Deserialize<PackageManifest>(Encoding.UTF8.GetString(bytes))
            ?? throw new InvalidDataException("manifest.json không hợp lệ.");

        string sig = "";
        var sigEntry = zip.GetEntry(SignatureEntry);
        if (sigEntry is not null)
        {
            using var ss = new StreamReader(sigEntry.Open());
            sig = ss.ReadToEnd().Trim();
        }
        return new VhwPackageReader(zip, manifest, bytes, sig);
    }

    /// <summary>Kiểm tra manifest cơ bản + chống path traversal + giới hạn kích thước.</summary>
    public Result ValidateStructure()
    {
        if (Manifest.SchemaVersion <= 0) return Result.Fail("schemaVersion không hợp lệ.");
        if (string.IsNullOrWhiteSpace(Manifest.PackageId)) return Result.Fail("Thiếu packageId.");
        if (Manifest.Files.Count == 0) return Result.Fail("Gói không có file nào.");
        if (Manifest.Files.Count > MaxFileCount) return Result.Fail("Số file trong gói quá lớn.");

        long total = 0;
        foreach (var f in Manifest.Files)
        {
            if (!PathValidation.IsSafeRelativePath(f.Destination))
                return Result.Fail($"Đích không an toàn: {f.Destination}");
            var entry = _zip.GetEntry(f.Source);
            if (entry is null) return Result.Fail($"Thiếu file trong gói: {f.Source}");
            if (entry.Length > MaxSingleFileBytes) return Result.Fail($"File quá lớn: {f.Source}");
            total += entry.Length;
            if (total > MaxTotalBytes) return Result.Fail("Tổng dung lượng gói quá lớn.");
        }
        return Result.Ok();
    }

    public bool VerifySignature(ISignatureService sig, string publicKeyPem)
        => sig.Verify(ManifestBytes, Signature, publicKeyPem);

    /// <summary>Kiểm SHA-256 từng payload khớp manifest.</summary>
    public async Task<Result> VerifyPayloadHashesAsync(IHashService hash, CancellationToken ct = default)
    {
        foreach (var f in Manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(f.Sha256)) continue;
            var entry = _zip.GetEntry(f.Source);
            if (entry is null) return Result.Fail($"Thiếu file: {f.Source}");
            await using var s = entry.Open();
            var actual = await hash.Sha256StreamAsync(s, ct).ConfigureAwait(false);
            if (!string.Equals(actual, f.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                return Result.Fail($"Sai SHA-256: {f.Source}");
        }
        return Result.Ok();
    }

    public Stream OpenPayload(string source)
    {
        var entry = _zip.GetEntry(source) ?? throw new FileNotFoundException($"Không có: {source}");
        return entry.Open();
    }

    /// <summary>Kích thước giải nén của một payload (byte).</summary>
    public long OpenPayloadLength(string source)
        => _zip.GetEntry(source)?.Length ?? 0;

    public void Dispose() => _zip.Dispose();
}

/// <summary>Tạo gói .vhwpack từ thư mục nguồn (chứa manifest.json + payload/).</summary>
public static class VhwPackageWriter
{
    /// <summary>Đọc manifest.json trong sourceDir, tính SHA-256 mỗi file, ký manifest, đóng ZIP.</summary>
    public static PackageManifest Create(string sourceDir, string outputPath,
        IHashService hash, ISignatureService? sig = null, string? privateKeyPem = null)
    {
        var manifestPath = Path.Combine(sourceDir, VhwPackageReader.ManifestEntry);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Thiếu manifest.json trong thư mục nguồn.", manifestPath);

        var manifest = VhwJson.Deserialize<PackageManifest>(File.ReadAllText(manifestPath))
            ?? throw new InvalidDataException("manifest.json không hợp lệ.");

        foreach (var f in manifest.Files)
        {
            if (!PathValidation.IsSafeRelativePath(f.Destination))
                throw new UnauthorizedAccessException($"Đích không an toàn: {f.Destination}");
            var src = Path.Combine(sourceDir, f.Source);
            if (!File.Exists(src)) throw new FileNotFoundException($"Thiếu payload: {f.Source}", src);
            f.Sha256 = hash.Sha256File(src);
        }

        var manifestBytes = Encoding.UTF8.GetBytes(VhwJson.Serialize(manifest));
        string signature = "";
        if (sig is not null && !string.IsNullOrWhiteSpace(privateKeyPem))
            signature = sig.SignBase64(manifestBytes, privateKeyPem!);

        var outDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        if (File.Exists(outputPath)) File.Delete(outputPath);

        using (var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create))
        {
            var me = zip.CreateEntry(VhwPackageReader.ManifestEntry, CompressionLevel.Optimal);
            using (var s = me.Open()) s.Write(manifestBytes);

            if (!string.IsNullOrEmpty(signature))
            {
                var se = zip.CreateEntry(VhwPackageReader.SignatureEntry, CompressionLevel.NoCompression);
                using var sw = new StreamWriter(se.Open());
                sw.Write(signature);
            }

            foreach (var f in manifest.Files)
            {
                var src = Path.Combine(sourceDir, f.Source);
                zip.CreateEntryFromFile(src, f.Source.Replace('\\', '/'), CompressionLevel.Optimal);
            }
        }
        return manifest;
    }
}
