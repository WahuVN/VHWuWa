using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.PackageTool;

/// <summary>Công cụ dòng lệnh tạo/ký/xác minh gói .vhwpack.</summary>
internal static class Program
{
    private static readonly IHashService Hash = new HashService();
    private static readonly ISignatureService Sig = new RsaSignatureService();

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0) { Usage(); return 1; }
            var cmd = args[0].ToLowerInvariant();
            var opt = ParseOptions(args.Skip(1));
            return cmd switch
            {
                "keygen" => KeyGen(opt),
                "pack" => Pack(opt),
                "verify" => Verify(opt),
                "list" => List(opt),
                _ => Fail($"Lệnh không hỗ trợ: {cmd}")
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static int KeyGen(Dictionary<string, string> o)
    {
        var dir = o.GetValueOrDefault("out", ".");
        Directory.CreateDirectory(dir);
        var (pub, priv) = Sig.GenerateKeyPair();
        var pubPath = Path.Combine(dir, "public_key.pem");
        var privPath = Path.Combine(dir, "private_key.pem");
        File.WriteAllText(pubPath, pub);
        File.WriteAllText(privPath, priv);
        Console.WriteLine($"✔ Public key : {pubPath}  (đưa vào Config/ của ứng dụng)");
        Console.WriteLine($"✔ Private key: {privPath}");
        Console.WriteLine("⚠ TUYỆT ĐỐI KHÔNG commit private_key.pem lên repository. Giữ trong GitHub Secrets.");
        return 0;
    }

    private static int Pack(Dictionary<string, string> o)
    {
        if (!o.TryGetValue("input", out var input)) return Fail("Thiếu --input <thư mục nguồn>");
        if (!o.TryGetValue("output", out var output)) return Fail("Thiếu --output <file.vhwpack>");
        string? privPem = null;
        if (o.TryGetValue("key", out var keyPath) && File.Exists(keyPath))
            privPem = File.ReadAllText(keyPath);

        var manifest = VhwPackageWriter.Create(input, output, Hash, privPem is null ? null : Sig, privPem);
        Console.WriteLine($"✔ Đã tạo gói: {output}");
        Console.WriteLine($"  packageId={manifest.PackageId}  version={manifest.Version}  files={manifest.Files.Count}");
        Console.WriteLine(privPem is null ? "  (chưa ký — dùng --key để ký)" : "  (đã ký số)");
        return 0;
    }

    private static int Verify(Dictionary<string, string> o)
    {
        if (!o.TryGetValue("file", out var file)) return Fail("Thiếu --file <file.vhwpack>");
        using var reader = VhwPackageReader.Open(file);
        var v = reader.ValidateStructure();
        Console.WriteLine($"Cấu trúc     : {(v.Success ? "OK" : "LỖI: " + v.Error)}");
        if (!v.Success) return 2;

        if (o.TryGetValue("pub", out var pub) && File.Exists(pub))
        {
            var ok = reader.VerifySignature(Sig, File.ReadAllText(pub));
            Console.WriteLine($"Chữ ký       : {(ok ? "HỢP LỆ" : "KHÔNG HỢP LỆ")}");
            if (!ok) return 3;
        }
        else Console.WriteLine("Chữ ký       : (bỏ qua — không có --pub)");

        var h = reader.VerifyPayloadHashesAsync(Hash).GetAwaiter().GetResult();
        Console.WriteLine($"SHA-256      : {(h.Success ? "KHỚP" : "SAI: " + h.Error)}");
        PrintManifest(reader.Manifest);
        return h.Success ? 0 : 4;
    }

    private static int List(Dictionary<string, string> o)
    {
        if (!o.TryGetValue("file", out var file)) return Fail("Thiếu --file <file.vhwpack>");
        using var reader = VhwPackageReader.Open(file);
        PrintManifest(reader.Manifest);
        var v = reader.ValidateStructure();
        if (!v.Success) Console.WriteLine("⚠ Cảnh báo cấu trúc: " + v.Error);
        return 0;
    }

    private static void PrintManifest(PackageManifest m)
    {
        Console.WriteLine($"  Gói   : {m.PackageName} ({m.PackageId}) v{m.Version} [{m.PackageType}]");
        if (!string.IsNullOrWhiteSpace(m.Author)) Console.WriteLine($"  Tác giả: {m.Author}");
        Console.WriteLine($"  Files : {m.Files.Count}");
        foreach (var f in m.Files)
            Console.WriteLine($"    {f.Source}  ->  {f.Destination}  [{f.Operation}]  {f.Sha256[..Math.Min(12, f.Sha256.Length)]}");
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> args)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var list = args.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].StartsWith("--")) continue;
            var key = list[i][2..];
            var val = (i + 1 < list.Count && !list[i + 1].StartsWith("--")) ? list[++i] : "true";
            d[key] = val;
        }
        return d;
    }

    private static int Fail(string msg)
    {
        Console.Error.WriteLine("LỖI: " + msg);
        Usage();
        return 1;
    }

    private static void Usage()
    {
        Console.WriteLine("""
        VHWuWa.PackageTool — tạo & xác minh gói .vhwpack

          keygen --out <dir>
          pack   --input <dir> --output <file.vhwpack> [--key private_key.pem]
          verify --file <file.vhwpack> [--pub public_key.pem]
          list   --file <file.vhwpack>
        """);
    }
}
