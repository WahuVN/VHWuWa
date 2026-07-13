using VHWuWa.Core.Models;

namespace VHWuWa.Core.Abstractions;

/// <summary>Ghi log (Serilog) + đọc log gần đây cho UI.</summary>
public interface ILogService
{
    void Info(string operation, string message);
    void Warn(string operation, string message);
    void Error(string operation, string message, Exception? ex = null);
    string LogDirectory { get; }
    IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null, string? search = null);
    void Clear();
}

/// <summary>Quản lý cấu hình ứng dụng + trạng thái cài đặt (LocalAppData).</summary>
public interface ISettingsService
{
    string AppDataDirectory { get; }
    string BackupsDirectory { get; }
    AppSettings Settings { get; }
    void Save();
    InstalledState LoadState();
    void SaveState(InstalledState state);
}

/// <summary>Nhận diện &amp; kiểm tra thư mục game (đọc Config/game.json).</summary>
public interface IGameDetectionService
{
    GameConfig GameConfig { get; }
    GameValidation Validate(string gamePath);
    /// <summary>Tự động dò thư mục game ở các vị trí phổ biến + Steam + Registry.</summary>
    IReadOnlyList<string> AutoDetect();
    string? DetectVersion(string gamePath);
}

/// <summary>Sao lưu &amp; khôi phục file gốc.</summary>
public interface IBackupService
{
    /// <summary>Tạo backup cho danh sách đích (tương đối game). Trả BackupManifest.</summary>
    BackupManifest CreateBackup(string gamePath, string operation, string packageId, string version,
        IEnumerable<string> destinations);
    Result Restore(string gamePath, string backupId);
    IReadOnlyList<BackupInfo> List();
    Result Delete(string backupId);
    string BackupsDirectory { get; }
}

/// <summary>Cài / gỡ gói .vhwpack (Việt hóa hoặc mod) — có kiểm tra, backup, rollback.</summary>
public interface IPackageInstallerService
{
    Task<Result> InstallAsync(string gamePath, string vhwpackPath,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
    Task<Result> UninstallAsync(string gamePath, string packageId, CancellationToken ct = default);
    /// <summary>Xem trước manifest + kiểm tra chữ ký/hash (không cài).</summary>
    Task<Result<PackageManifest>> InspectAsync(string vhwpackPath, CancellationToken ct = default);
}

/// <summary>Quản lý mod: liệt kê, bật/tắt, phát hiện xung đột.</summary>
public interface IModService
{
    IReadOnlyList<ModInfo> ListInstalled(string gamePath);
    Result SetEnabled(string gamePath, string packageId, bool enabled);
    IReadOnlyList<string> DetectConflicts(string vhwpackPath);
}

/// <summary>Đổi font trong game (quản lý bằng manifest, có backup).</summary>
public interface IFontService
{
    Task<Result> ApplyFontAsync(string gamePath, string vhwpackPath, CancellationToken ct = default);
    Task<Result> RestoreDefaultAsync(string gamePath, CancellationToken ct = default);
}

/// <summary>Render ảnh xem trước một file font (.ttf/.otf/.ttc) với chữ mẫu tiếng Việt.</summary>
public interface IFontPreviewService
{
    /// <summary>Trả về PNG (byte[]) hoặc null nếu không đọc được font / không phải Windows.</summary>
    byte[]? RenderPreview(string fontFilePath, string sampleText, int fontSize = 30);
}

/// <summary>Chỉnh cấu hình đồ họa (đọc Config/graphics.json), có preset + backup.</summary>
public interface IGraphicsService
{
    GraphicsConfig Config { get; }
    bool IsSupported { get; }
    Dictionary<string, string> ReadCurrent(string gamePath);
    Result Apply(string gamePath, Dictionary<string, string> values);
    Result ApplyPreset(string gamePath, string presetName);
    string? ConfigFilePath(string gamePath);
}

/// <summary>Kiểm tra &amp; tải cập nhật ứng dụng từ GitHub Releases.</summary>
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);
    Task<Result<string>> DownloadAsync(UpdateManifest manifest, string destDir,
        IProgress<double>? progress = null, CancellationToken ct = default);
}
