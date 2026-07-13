using System.Text;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public class HashServiceTests
{
    [Fact]
    public void Sha256_Of_abc_Is_Known()
    {
        var h = new HashService();
        // SHA-256("abc")
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            h.Sha256Bytes(Encoding.UTF8.GetBytes("abc")));
    }

    [Fact]
    public void Verify_Empty_Expected_ReturnsTrue()
    {
        var tmp = Path.GetTempFileName();
        try { Assert.True(new HashService().Verify(tmp, "")); }
        finally { File.Delete(tmp); }
    }
}

public class PathValidationTests
{
    [Theory]
    [InlineData("Data/text.pak", true)]
    [InlineData("a/b/c.bin", true)]
    [InlineData("../evil.txt", false)]
    [InlineData("Data/../../evil", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("C:\\Windows\\x", false)]
    [InlineData("\\\\server\\share", false)]
    [InlineData("", false)]
    public void IsSafeRelativePath_Works(string p, bool expected)
        => Assert.Equal(expected, PathValidation.IsSafeRelativePath(p));

    [Fact]
    public void ResolveInsideRoot_Throws_On_Traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "vhw_root");
        Assert.Throws<UnauthorizedAccessException>(() =>
            PathValidation.ResolveInsideRoot(root, "../outside.txt"));
    }

    [Fact]
    public void ResolveInsideRoot_Ok_For_Safe()
    {
        var root = Path.Combine(Path.GetTempPath(), "vhw_root");
        var resolved = PathValidation.ResolveInsideRoot(root, "Data/text.pak");
        Assert.StartsWith(Path.GetFullPath(root), resolved, StringComparison.OrdinalIgnoreCase);
    }
}

public class VersionComparerTests
{
    [Theory]
    [InlineData("1.1.0", "1.0.0", true)]
    [InlineData("v1.2.0", "1.2.0", false)]
    [InlineData("1.0.0", "1.0.1", false)]
    [InlineData("2.0", "1.9.9", true)]
    public void IsNewer_Works(string cand, string cur, bool expected)
        => Assert.Equal(expected, VersionComparer.IsNewer(cand, cur));
}

public class SignatureTests
{
    [Fact]
    public void Sign_Then_Verify_Roundtrip_And_Tamper_Fails()
    {
        var sig = new RsaSignatureService();
        var (pub, priv) = sig.GenerateKeyPair(2048);
        var data = Encoding.UTF8.GetBytes("hello vhwpack");
        var s = sig.SignBase64(data, priv);

        Assert.True(sig.Verify(data, s, pub));
        Assert.False(sig.Verify(Encoding.UTF8.GetBytes("hello vhwpack!"), s, pub));
    }
}
