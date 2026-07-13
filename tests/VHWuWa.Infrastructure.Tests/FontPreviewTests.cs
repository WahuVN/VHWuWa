using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class FontPreviewTests
{
    private static string? FindSystemFont()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        foreach (var name in new[] { "arial.ttf", "segoeui.ttf", "tahoma.ttf", "calibri.ttf" })
        {
            var p = Path.Combine(dir, name);
            if (File.Exists(p)) return p;
        }
        return Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.ttf").FirstOrDefault()
            : null;
    }

    [Fact]
    public void RenderPreview_ProducesPngBytes_ForSystemFont()
    {
        if (!OperatingSystem.IsWindows()) return; // chỉ chạy trên Windows
        var font = FindSystemFont();
        if (font is null) return; // không có font hệ thống thì bỏ qua

        var svc = new FontPreviewService();
        var png = svc.RenderPreview(font, "Tiếng Việt Wuthering Waves");

        Assert.NotNull(png);
        Assert.True(png!.Length > 100);
        // Chữ ký PNG: 89 50 4E 47
        Assert.Equal(0x89, png[0]);
        Assert.Equal(0x50, png[1]);
        Assert.Equal(0x4E, png[2]);
        Assert.Equal(0x47, png[3]);
    }

    [Fact]
    public void RenderPreview_ReturnsNull_ForMissingFile()
    {
        var svc = new FontPreviewService();
        var png = svc.RenderPreview(Path.Combine(Path.GetTempPath(), "khong-ton-tai.ttf"), "x");
        Assert.Null(png);
    }
}
