using System.Text;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public class ManifestTests
{
    [Fact]
    public void Manifest_Json_Roundtrip_Keeps_Fields()
    {
        var m = new PackageManifest
        {
            PackageId = "vietnamese-pack",
            PackageName = "Bản Việt hóa",
            PackageType = PackageType.Translation,
            Version = "1.0.0",
            SupportedGameVersions = { "1.0.0" },
            Files = { new PackageFileEntry { Source = "payload/text.pak", Destination = "Data/text.pak", Operation = FileOperation.Replace } }
        };
        var json = VhwJson.Serialize(m);
        Assert.Contains("\"translation\"", json); // enum dạng chuỗi camelCase
        var back = VhwJson.Deserialize<PackageManifest>(json)!;
        Assert.Equal("vietnamese-pack", back.PackageId);
        Assert.Equal(FileOperation.Replace, back.Files[0].Operation);
        Assert.Equal("Data/text.pak", back.Files[0].Destination);
    }
}

public class VhwPackageTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "vhw_pkg_" + Guid.NewGuid().ToString("N"));

    private string BuildSource(string destination)
    {
        var src = Path.Combine(_work, "src");
        Directory.CreateDirectory(Path.Combine(src, "payload"));
        File.WriteAllText(Path.Combine(src, "payload", "text.pak"), "noi dung viet hoa demo");
        var manifest = new PackageManifest
        {
            PackageId = "demo-pack",
            PackageName = "Demo",
            PackageType = PackageType.Translation,
            Version = "1.0.0",
            Files = { new PackageFileEntry { Source = "payload/text.pak", Destination = destination } }
        };
        File.WriteAllText(Path.Combine(src, "manifest.json"), VhwJson.Serialize(manifest));
        return src;
    }

    [Fact]
    public async Task Create_Open_Verify_Roundtrip()
    {
        var hash = new HashService();
        var sig = new RsaSignatureService();
        var (pub, priv) = sig.GenerateKeyPair(2048);

        var src = BuildSource("Data/text.pak");
        var outFile = Path.Combine(_work, "demo.vhwpack");
        var manifest = VhwPackageWriter.Create(src, outFile, hash, sig, priv);
        Assert.False(string.IsNullOrEmpty(manifest.Files[0].Sha256));

        using var reader = VhwPackageReader.Open(outFile);
        Assert.True(reader.ValidateStructure().Success);
        Assert.True(reader.VerifySignature(sig, pub));
        Assert.False(reader.VerifySignature(sig, sig.GenerateKeyPair(2048).publicPem)); // sai key
        Assert.True((await reader.VerifyPayloadHashesAsync(hash)).Success);
    }

    [Fact]
    public void ValidateStructure_Rejects_Path_Traversal()
    {
        var hash = new HashService();
        // manifest có đích ../ -> Writer phải ném ngay khi tạo
        var src = Path.Combine(_work, "bad");
        Directory.CreateDirectory(Path.Combine(src, "payload"));
        File.WriteAllText(Path.Combine(src, "payload", "x.bin"), "x");
        var m = new PackageManifest
        {
            PackageId = "bad",
            Files = { new PackageFileEntry { Source = "payload/x.bin", Destination = "../../evil.bin" } }
        };
        File.WriteAllText(Path.Combine(src, "manifest.json"), VhwJson.Serialize(m));
        Assert.Throws<UnauthorizedAccessException>(() =>
            VhwPackageWriter.Create(src, Path.Combine(_work, "bad.vhwpack"), hash));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_work)) Directory.Delete(_work, true); } catch { }
    }
}
