using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;
using Wpf.Ui.Appearance;

namespace VHWuWa.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;

    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _gameStatus = "Chưa chọn thư mục game";
    [ObservableProperty] private bool _gameOk;
    [ObservableProperty] private string _updateStatus = "Chưa kiểm tra cập nhật";
    [ObservableProperty] private bool _isDark = true;

    public MainViewModel(ISettingsService settings, IGameDetectionService detect)
    {
        _settings = settings;
        _detect = detect;
        _appVersion = "v" + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0");
        _isDark = !settings.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            GameOk = false;
            GameStatus = "Chưa chọn thư mục game";
            return;
        }
        var v = _detect.Validate(path);
        GameOk = v.IsValid;
        GameStatus = v.IsValid ? "Game hợp lệ" : "Đường dẫn game chưa hợp lệ";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDark = !IsDark;
        var theme = IsDark ? "Dark" : "Light";
        _settings.Settings.Theme = theme;
        _settings.Save();
        ApplicationThemeManager.Apply(IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }
}
