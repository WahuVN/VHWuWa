using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class InstallViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IPackageInstallerService _installer;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _packagePath = "";
    [ObservableProperty] private string _summary = "Chọn một gói .vhwpack để xem chi tiết trước khi cài.";
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private bool _canInstall;

    public ObservableCollection<string> Files { get; } = new();

    public InstallViewModel(ISettingsService settings, IPackageInstallerService installer)
    {
        _settings = settings; _installer = installer;
    }

    public void OnActivated() { }

    [RelayCommand]
    private async Task PickPackageAsync()
    {
        var dlg = new OpenFileDialog { Title = "Chọn gói Việt hóa", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;
        PackagePath = dlg.FileName;
        Files.Clear();
        var r = await _installer.InspectAsync(PackagePath);
        if (!r.Success) { Summary = "Gói không hợp lệ: " + r.Error; CanInstall = false; return; }
        var m = r.Value!;
        Summary = $"{m.PackageName} v{m.Version} — {m.Files.Count} file — tác giả: {m.Author}";
        foreach (var f in m.Files) Files.Add($"{f.Destination}  [{f.Operation}]");
        CanInstall = true;
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        var gamePath = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(gamePath)) { Summary = "Chưa chọn thư mục game (ở Trang chủ)."; return; }
        _cts = new CancellationTokenSource();
        Busy = true; Progress = 0; ProgressText = "Bắt đầu...";
        var progress = new Progress<InstallProgress>(p =>
        {
            Progress = p.Percent;
            ProgressText = $"{p.Completed}/{p.Total} — {p.CurrentFile}";
        });
        try
        {
            var r = await _installer.InstallAsync(gamePath, PackagePath, progress, _cts.Token);
            Summary = r.Success ? "Cài đặt hoàn tất." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; _cts?.Dispose(); _cts = null; }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    partial void OnCanInstallChanged(bool value) => InstallCommand.NotifyCanExecuteChanged();
}
