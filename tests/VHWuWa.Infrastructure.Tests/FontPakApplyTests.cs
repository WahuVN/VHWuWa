using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class FontPakApplyTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "vhw_font_" + Guid.NewGuid().ToString("N"));
    private readonly string _game;
    private readonly FontService _svc;

    public FontPakApplyTests()
    {
        _game = Path.Combine(_work, "game");
        Directory.CreateDirectory(_game);
        // FontService chỉ dùng installer/settings cho các API khác; ApplyFontPak không cần chúng.
        _svc = new FontService(installer: null!, settings: null!);
    }

    private string VhDir => Path.Combine(_game, "Client", "Binaries", "Win64", "wuwaVietHoa");

    [Fact]
    public async Task ApplyFontPak_Fails_WhenNotInstalled()
    {
        var pak = Path.Combine(_work, "MyFont_100_P.pak");
        File.WriteAllText(pak, "PAK");
        var r = await _svc.ApplyFontPakAsync(_game, pak);
        Assert.False(r.Success);
        Assert.Contains("wuwaVietHoa", r.Error);
    }

    [Fact]
    public async Task ApplyFontPak_ReplacesOldFontPak_AndCurrentReflectsIt()
    {
        Directory.CreateDirectory(VhDir);
        // pak text (priority 99) + font cũ (priority 100) đã có sẵn
        File.WriteAllText(Path.Combine(VhDir, "WuWaVH_99_P.pak"), "TEXT");
        File.WriteAllText(Path.Combine(VhDir, "OldFont_100_P.pak"), "OLD");

        var newPak = Path.Combine(_work, "NewFont_100_P.pak");
        File.WriteAllText(newPak, "NEW");

        var r = await _svc.ApplyFontPakAsync(_game, newPak);
        Assert.True(r.Success, r.Error);

        Assert.False(File.Exists(Path.Combine(VhDir, "OldFont_100_P.pak")));   // font cũ bị xóa
        Assert.True(File.Exists(Path.Combine(VhDir, "NewFont_100_P.pak")));    // font mới đã vào
        Assert.True(File.Exists(Path.Combine(VhDir, "WuWaVH_99_P.pak")));      // pak text còn nguyên
        Assert.Equal("NewFont_100_P.pak", _svc.CurrentFontPak(_game));

        var rm = await _svc.RemoveFontPaksAsync(_game);
        Assert.True(rm.Success);
        Assert.Null(_svc.CurrentFontPak(_game));
        Assert.True(File.Exists(Path.Combine(VhDir, "WuWaVH_99_P.pak")));      // vẫn không đụng pak text
    }

    public void Dispose()
    {
        try { Directory.Delete(_work, true); } catch { }
    }
}
