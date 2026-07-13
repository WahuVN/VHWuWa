using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class ModViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IModService _mods;
    private readonly IPackageInstallerService _installer;

    [ObservableProperty] private ModInfo? _selectedMod;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _busy;

    public ObservableCollection<ModInfo> Mods { get; } = new();

    public ModViewModel(ISettingsService settings, IModService mods, IPackageInstallerService installer)
    {
        _settings = settings; _mods = mods; _installer = installer;
    }

    public void OnActivated() => Refresh();

    private void Refresh()
    {
        Mods.Clear();
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        foreach (var m in _mods.ListInstalled(path)) Mods.Add(m);
        Message = Mods.Count == 0 ? "Chưa có mod nào." : $"{Mods.Count} mod đã cài.";
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn mod", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;

        var conflicts = _mods.DetectConflicts(dlg.FileName);
        if (conflicts.Count > 0)
            Message = "Cảnh báo xung đột: " + string.Join("; ", conflicts.Take(3));

        Busy = true;
        try
        {
            var r = await _installer.InstallAsync(path, dlg.FileName);
            Message = r.Success ? "Đã cài mod." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        if (SelectedMod is null) { Message = "Chọn một mod."; return; }
        Busy = true;
        try
        {
            var r = await _installer.UninstallAsync(_settings.Settings.GamePath, SelectedMod.PackageId);
            Message = r.Success ? "Đã gỡ mod." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private void Toggle()
    {
        if (SelectedMod is null) { Message = "Chọn một mod."; return; }
        var r = _mods.SetEnabled(_settings.Settings.GamePath, SelectedMod.PackageId, !SelectedMod.Enabled);
        Message = r.Success ? "Đã đổi trạng thái mod." : "Lỗi: " + r.Error;
        Refresh();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var p = _settings.Settings.GamePath;
        if (Directory.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true });
    }
}
