using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using VHWuWa.Core.Abstractions;

namespace VHWuWa.Infrastructure;

/// <summary>
/// Render ảnh xem trước font bằng System.Drawing (chỉ chạy trên Windows).
/// Không cài font vào hệ thống — dùng PrivateFontCollection trong bộ nhớ.
/// </summary>
public sealed class FontPreviewService : IFontPreviewService
{
    public byte[]? RenderPreview(string fontFilePath, string sampleText, int fontSize = 30)
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (string.IsNullOrWhiteSpace(fontFilePath) || !File.Exists(fontFilePath)) return null;
        if (string.IsNullOrWhiteSpace(sampleText)) sampleText = "Tiếng Việt";

        try
        {
            return RenderWindows(fontFilePath, sampleText, Math.Clamp(fontSize, 8, 96));
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static byte[] RenderWindows(string fontFilePath, string sampleText, int fontSize)
    {
        using var pfc = new PrivateFontCollection();
        pfc.AddFontFile(fontFilePath);
        var family = pfc.Families[0];

        const int width = 780;
        const int height = 240;
        using var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(21, 27, 41));      // nền navy khớp theme
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        using var title = new Font(family, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var body = new Font(family, fontSize * 0.7f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var small = new Font(family, fontSize * 0.55f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var white = new SolidBrush(Color.White);
        using var accent = new SolidBrush(Color.FromArgb(124, 107, 255)); // #7c6bff

        g.DrawString(family.Name, small, accent, new PointF(16, 12));
        g.DrawString(sampleText, title, white, new RectangleF(16, 44, width - 32, 70));
        g.DrawString("Tiếng Việt có dấu: ăâđêôơư ÀÁẢÃẠ Ệ Ỡ Ự Ýỳỷ",
            body, white, new RectangleF(16, 116, width - 32, 50));
        g.DrawString("0123456789 — Wuthering Waves — nWaVzZ",
            body, white, new RectangleF(16, 168, width - 32, 50));

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
