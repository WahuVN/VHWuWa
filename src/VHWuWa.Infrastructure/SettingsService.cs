using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

/// <summary>Đường dẫn cố định của ứng dụng.</summary>
public static class AppPaths
{
    public static string AppData => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VHWuWa");

    public static string DefaultConfigDir => Path.Combine(AppContext.BaseDirectory, "Config");
}

public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly string _statePath;

    public string AppDataDirectory { get; }
    public string BackupsDirectory { get; }
    public AppSettings Settings { get; private set; }

    public SettingsService(string? appDataDir = null)
    {
        AppDataDirectory = appDataDir ?? AppPaths.AppData;
        Directory.CreateDirectory(AppDataDirectory);
        BackupsDirectory = Path.Combine(AppDataDirectory, "Backups");
        Directory.CreateDirectory(BackupsDirectory);

        _settingsPath = Path.Combine(AppDataDirectory, "settings.json");
        _statePath = Path.Combine(AppDataDirectory, "installed-state.json");
        Settings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
                return VhwJson.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings();
        }
        catch { /* file hỏng -> dùng mặc định */ }
        return new AppSettings();
    }

    public void Save()
        => File.WriteAllText(_settingsPath, VhwJson.Serialize(Settings));

    public InstalledState LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return VhwJson.Deserialize<InstalledState>(File.ReadAllText(_statePath)) ?? new InstalledState();
        }
        catch { }
        return new InstalledState();
    }

    public void SaveState(InstalledState state)
        => File.WriteAllText(_statePath, VhwJson.Serialize(state));
}
