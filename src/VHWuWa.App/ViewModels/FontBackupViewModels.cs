using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    [ObservableProperty] private string _message = "Chọn gói font (.vhwpack) để áp dụng.";
    [ObservableProperty] private string _currentFont = "Mặc định";
    [ObservableProperty] private bool _busy;

    public FontViewModel(ISettingsService settings, IFontService fonts)
    {
        _settings = settings; _fonts = fonts;
    }

    public void OnActivated()
    {
        var state = _settings.LoadState();
        var f = state.InstalledPackages.FirstOrDefault(p => p.PackageType == PackageType.Font);
        CurrentFont = f?.PackageName ?? "Mặc định";
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
