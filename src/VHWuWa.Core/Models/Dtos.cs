namespace VHWuWa.Core.Models;

/// <summary>Cấu hình ứng dụng (lưu %LocalAppData%/VHWuWa/settings.json).</summary>
public sealed class AppSettings
{
    public string GamePath { get; set; } = "";
    public string Theme { get; set; } = "Dark";       // Dark | Light | System
    public bool AutoCheckUpdate { get; set; } = true;
    public DateTimeOffset? LastUpdateCheck { get; set; }
}

/// <summary>Kết quả kiểm tra thư mục game.</summary>
public sealed class GameValidation
{
    public bool IsValid { get; set; }
    public string GamePath { get; set; } = "";
    public string? DetectedVersion { get; set; }
    public List<string> MissingFiles { get; set; } = new();
    public string Message { get; set; } = "";
}

/// <summary>Thông tin mod hiển thị trên UI.</summary>
public sealed class ModInfo
{
    public string PackageId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset InstalledAt { get; set; }
    public bool Enabled { get; set; }
    public bool Installed { get; set; }
    public List<string> Conflicts { get; set; } = new();
}

/// <summary>Thông tin backup hiển thị trên UI.</summary>
public sealed class BackupInfo
{
    public string Id { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Operation { get; set; } = "";
    public string PackageId { get; set; } = "";
    public string Version { get; set; } = "";
    public int FileCount { get; set; }
    public string Path { get; set; } = "";
}

/// <summary>Kết quả kiểm tra cập nhật.</summary>
public sealed class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public string CurrentVersion { get; set; } = "";
    public UpdateManifest? Manifest { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>Một dòng log để hiển thị trên trang Nhật ký.</summary>
public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}
