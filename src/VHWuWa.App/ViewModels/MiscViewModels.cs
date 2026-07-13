using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;

namespace VHWuWa.App.ViewModels;

public partial class GuideViewModel : ObservableObject
{
    private readonly string _guidesDir = Path.Combine(AppContext.BaseDirectory, "Guides", "vi-VN");
    private List<string> _all = new();

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private string? _selected;
    [ObservableProperty] private string _content = "Chọn một mục hướng dẫn ở bên trái.";

    public ObservableCollection<string> Guides { get; } = new();

    public void OnActivated()
    {
        _all = Directory.Exists(_guidesDir)
            ? Directory.GetFiles(_guidesDir, "*.md").Select(Path.GetFileName).OfType<string>().OrderBy(x => x).ToList()
            : new();
        ApplyFilter();
    }

    partial void OnSearchChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Guides.Clear();
        foreach (var g in _all)
            if (string.IsNullOrWhiteSpace(Search) || g.Contains(Search, StringComparison.OrdinalIgnoreCase))
                Guides.Add(g);
    }

    partial void OnSelectedChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var path = Path.Combine(_guidesDir, value);
            Content = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "(Không đọc được nội dung.)";
        }
        catch (Exception ex) { Content = "Lỗi đọc hướng dẫn: " + ex.Message; }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(_guidesDir)) Process.Start(new ProcessStartInfo(_guidesDir) { UseShellExecute = true });
    }
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IUpdateService _update;
    private readonly ILogService _log;
    private readonly MainViewModel _main;

    [ObservableProperty] private string _gamePath = "";
    [ObservableProperty] private bool _isDark = true;
    [ObservableProperty] private bool _autoCheckUpdate = true;
    [ObservableProperty] private string _appVersion = "";
    [ObservableProperty] private string _updateMessage = "";
    [ObservableProperty] private bool _busy;

    public SettingsViewModel(ISettingsService settings, IUpdateService update, ILogService log, MainViewModel main)
    {
        _settings = settings; _update = update; _log = log; _main = main;
        GamePath = settings.Settings.GamePath;
        IsDark = !settings.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        AutoCheckUpdate = settings.Settings.AutoCheckUpdate;
        AppVersion = _main.AppVersion;
    }

    public void OnActivated() => GamePath = _settings.Settings.GamePath;

    [RelayCommand]
    private void ChooseFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Chọn thư mục game" };
        if (dlg.ShowDialog() != true) return;
        GamePath = dlg.FolderName;
        _settings.Settings.GamePath = GamePath;
        _settings.Save();
        _main.RefreshStatus();
    }

    partial void OnIsDarkChanged(bool value)
    {
        _settings.Settings.Theme = value ? "Dark" : "Light";
        _settings.Save();
        App.ApplyTheme(_settings.Settings.Theme);
        _main.IsDark = value;
    }

    partial void OnAutoCheckUpdateChanged(bool value)
    {
        _settings.Settings.AutoCheckUpdate = value;
        _settings.Save();
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        Busy = true; UpdateMessage = "Đang kiểm tra...";
        try
        {
            var r = await _update.CheckAsync();
            UpdateMessage = r.Message;
            _main.UpdateStatus = r.UpdateAvailable ? $"Có bản mới {r.Manifest?.Version}" : "Đã mới nhất";
        }
        finally { Busy = false; }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        if (Directory.Exists(_log.LogDirectory))
            Process.Start(new ProcessStartInfo(_log.LogDirectory) { UseShellExecute = true });
    }
}

public partial class LogViewModel : ObservableObject
{
    private readonly ILogService _log;

    [ObservableProperty] private string _levelFilter = "Tất cả";
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private string _message = "";

    public ObservableCollection<Core.Models.LogEntry> Entries { get; } = new();
    public string[] Levels { get; } = { "Tất cả", "Info", "Warn", "Error" };

    public LogViewModel(ILogService log) => _log = log;

    public void OnActivated() => Refresh();

    [RelayCommand]
    private void Refresh()
    {
        Entries.Clear();
        foreach (var e in _log.ReadRecent(500, LevelFilter, Search)) Entries.Add(e);
        Message = $"{Entries.Count} dòng.";
    }

    [RelayCommand]
    private void Clear()
    {
        _log.Clear();
        Refresh();
    }

    [RelayCommand]
    private void Export()
    {
        var dlg = new SaveFileDialog { FileName = "vhwuwa-log.txt", Filter = "Text (*.txt)|*.txt" };
        if (dlg.ShowDialog() != true) return;
        var sb = new StringBuilder();
        foreach (var e in _log.ReadRecent(5000, LevelFilter, Search))
            sb.AppendLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss} [{e.Level}] {e.Message}");
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        Message = "Đã xuất log.";
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(_log.LogDirectory))
            Process.Start(new ProcessStartInfo(_log.LogDirectory) { UseShellExecute = true });
    }
}
