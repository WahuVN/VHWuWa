using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class FontViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IFontService _fonts;
    private readonly IFontPreviewService _preview;

    [ObservableProperty] private string _message = "Chọn gói font (.vhwpack) để áp dụng.";
    [ObservableProperty] private string _currentFont = "Mặc định";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _sampleText = "Tiếng Việt Wuthering Waves";
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _previewMessage = "Chọn file font (.ttf/.otf/.ttc) để xem trước.";

    public ObservableCollection<FontLibraryItem> Library { get; } = new();
    [ObservableProperty] private FontLibraryItem? _selectedLibraryFont;
    [ObservableProperty] private string _libraryMessage = "";

    private static string FontDir => Path.Combine(AppContext.BaseDirectory, "Fonts");

    public FontViewModel(ISettingsService settings, IFontService fonts, IFontPreviewService preview)
    {
        _settings = settings; _fonts = fonts; _preview = preview;
        LoadLibrary();
    }

    private void LoadLibrary()
    {
        Library.Clear();
        var catalog = Path.Combine(FontDir, "fonts.json");
        if (!File.Exists(catalog))
        {
            LibraryMessage = "Chưa có thư viện font (thiếu thư mục Fonts\\). Tải thêm font để chọn.";
            return;
        }
        try
        {
            using var fs = File.OpenRead(catalog);
            var doc = System.Text.Json.JsonDocument.Parse(fs);
            foreach (var e in doc.RootElement.GetProperty("fonts").EnumerateArray())
            {
                Library.Add(new FontLibraryItem(
                    e.GetProperty("name").GetString() ?? "",
                    e.TryGetProperty("pak", out var p) ? p.GetString() ?? "" : "",
                    e.TryGetProperty("src", out var s) ? s.GetString() ?? "" : "",
                    e.TryGetProperty("sizeKb", out var k) ? k.GetDouble() : 0));
            }
            LibraryMessage = $"Thư viện có {Library.Count} font tiếng Việt.";
        }
        catch (Exception ex) { LibraryMessage = "Lỗi đọc thư viện font: " + ex.Message; }
    }

    public void OnActivated()
    {
        var path = _settings.Settings.GamePath;
        var pak = string.IsNullOrWhiteSpace(path) ? null : _fonts.CurrentFontPak(path);
        CurrentFont = pak ?? "Mặc định";
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn font", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;
        Busy = true;
        try
        {
            var r = await _fonts.ApplyFontAsync(path, dlg.FileName);
            Message = r.Success ? "Đã áp dụng font." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        Busy = true;
        try
        {
            var r = await _fonts.RestoreDefaultAsync(_settings.Settings.GamePath);
            Message = r.Success ? "Đã khôi phục font mặc định." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private void PreviewFont()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Chọn font để xem trước",
            Filter = "Font (*.ttf;*.otf;*.ttc)|*.ttf;*.otf;*.ttc|Tất cả (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        RenderPreview(dlg.FileName);
    }

    private string? _lastFontPath;

    private void RenderPreview(string fontPath)
    {
        _lastFontPath = fontPath;
        var png = _preview.RenderPreview(fontPath, SampleText);
        if (png is null)
        {
            PreviewImage = null;
            PreviewMessage = "Không đọc được font này.";
            return;
        }
        using var ms = new MemoryStream(png);
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        PreviewImage = img;
        PreviewMessage = Path.GetFileName(fontPath);
    }

    partial void OnSampleTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(_lastFontPath)) RenderPreview(_lastFontPath);
    }

    partial void OnSelectedLibraryFontChanged(FontLibraryItem? value)
    {
        if (value is null) return;
        if (!string.IsNullOrEmpty(value.Src) && File.Exists(value.Src))
            RenderPreview(value.Src);
        else
            PreviewMessage = value.Name + " (không có file nguồn để xem trước)";
    }

    [RelayCommand]
    private async Task ApplyLibraryFontAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { LibraryMessage = "Chưa chọn thư mục game."; return; }
        if (SelectedLibraryFont is null) { LibraryMessage = "Hãy chọn 1 font trong danh sách."; return; }
        var pak = Path.Combine(FontDir, SelectedLibraryFont.Pak);
        Busy = true;
        try
        {
            var r = await _fonts.ApplyFontPakAsync(path, pak);
            LibraryMessage = r.Success
                ? $"Đã áp font: {SelectedLibraryFont.Name}. Khởi động lại game để thấy."
                : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private async Task RemoveFontAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { LibraryMessage = "Chưa chọn thư mục game."; return; }
        Busy = true;
        try
        {
            var r = await _fonts.RemoveFontPaksAsync(path);
            LibraryMessage = r.Success ? "Đã gỡ font (về font mặc định của bản VH)." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }
}

/// <summary>1 mục trong thư viện font (Fonts/fonts.json).</summary>
public sealed record FontLibraryItem(string Name, string Pak, string Src, double SizeKb)
{
    public string Display => SizeKb > 0 ? $"{Name}  ·  {SizeKb:0} KB" : Name;
}

public partial class BackupViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;

    [ObservableProperty] private BackupInfo? _selected;
    [ObservableProperty] private string _message = "";

    public ObservableCollection<BackupInfo> Backups { get; } = new();

    public BackupViewModel(ISettingsService settings, IBackupService backup)
    {
        _settings = settings; _backup = backup;
    }

    public void OnActivated() => Refresh();

    private void Refresh()
    {
        Backups.Clear();
        foreach (var b in _backup.List()) Backups.Add(b);
        Message = Backups.Count == 0 ? "Chưa có bản sao lưu." : $"{Backups.Count} bản sao lưu.";
    }

    [RelayCommand]
    private void RestoreSelected()
    {
        if (Selected is null) { Message = "Chọn một bản sao lưu."; return; }
        var r = _backup.Restore(_settings.Settings.GamePath, Selected.Id);
        Message = r.Success ? "Đã khôi phục file gốc." : "Lỗi: " + r.Error;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (Selected is null) { Message = "Chọn một bản sao lưu."; return; }
        var r = _backup.Delete(Selected.Id);
        Message = r.Success ? "Đã xóa bản sao lưu." : "Lỗi: " + r.Error;
        Refresh();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var dir = Selected?.Path ?? _backup.BackupsDirectory;
        if (Directory.Exists(dir)) Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }

    [RelayCommand]
    private void RefreshList() => Refresh();
}
