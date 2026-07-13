using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;
    private readonly IPackageInstallerService _installer;
    private readonly ILogService _log;
    private readonly MainViewModel _main;

    [ObservableProperty] private string _gamePath = "";
    [ObservableProperty] private string _gameName = "";
    [ObservableProperty] private string _detectedVersion = "Chưa xác định";
    [ObservableProperty] private string _translationStatus = "Chưa cài";
    [ObservableProperty] private string _translationVersion = "-";
    [ObservableProperty] private string _pathStatus = "Chưa chọn";
    [ObservableProperty] private bool _pathOk;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _busy;

    public HomeViewModel(ISettingsService settings, IGameDetectionService detect,
        IPackageInstallerService installer, ILogService log, MainViewModel main)
    {
        _settings = settings; _detect = detect; _installer = installer; _log = log; _main = main;
        GameName = string.IsNullOrWhiteSpace(detect.GameConfig.GameName) ? "Game" : detect.GameConfig.GameName;
        OnActivated();
    }

    public void OnActivated()
    {
        GamePath = _settings.Settings.GamePath;
        Refresh();
    }

    private void Refresh()
    {
        var v = _detect.Validate(GamePath);
        PathOk = v.IsValid;
        PathStatus = string.IsNullOrWhiteSpace(GamePath) ? "Chưa chọn"
            : v.IsValid ? "Hợp lệ" : v.Message;
        DetectedVersion = v.DetectedVersion ?? "Chưa xác định";

        var state = _settings.LoadState();
        var tr = state.InstalledPackages.FirstOrDefault(p => p.PackageType == PackageType.Translation);
        TranslationStatus = tr is null ? "Chưa cài" : "Đã cài";
        TranslationVersion = tr?.Version ?? "-";
        _main.RefreshStatus();
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Chọn thư mục game" };
        if (dlg.ShowDialog() == true) SetPath(dlg.FolderName);
    }

    [RelayCommand]
    private void AutoDetect()
    {
        var found = _detect.AutoDetect();
        if (found.Count > 0) { SetPath(found[0]); Message = "Đã tự tìm thấy game."; }
        else Message = "Không tự tìm thấy game. Hãy chọn thủ công.";
    }

    public void SetPath(string path)
    {
        GamePath = path;
        _settings.Settings.GamePath = path;
        _settings.Save();
        Refresh();
    }

    [RelayCommand]
    private void CheckFiles()
    {
        var v = _detect.Validate(GamePath);
        Message = v.IsValid ? "Tất cả file bắt buộc đều có." : v.Message;
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(GamePath))
            Process.Start(new ProcessStartInfo(GamePath) { UseShellExecute = true });
        else Message = "Thư mục game không tồn tại.";
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        if (!PathOk) { Message = "Đường dẫn game chưa hợp lệ."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn gói Việt hóa", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;
        Busy = true; Message = "Đang cài Việt hóa...";
        try
        {
            var r = await _installer.InstallAsync(GamePath, dlg.FileName);
            Message = r.Success ? "Cài Việt hóa thành công." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        var state = _settings.LoadState();
        var tr = state.InstalledPackages.FirstOrDefault(p => p.PackageType == PackageType.Translation);
        if (tr is null) { Message = "Chưa cài Việt hóa."; return; }
        Busy = true; Message = "Đang gỡ Việt hóa...";
        try
        {
            var r = await _installer.UninstallAsync(GamePath, tr.PackageId);
            Message = r.Success ? "Đã gỡ Việt hóa và khôi phục file gốc." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }
}
