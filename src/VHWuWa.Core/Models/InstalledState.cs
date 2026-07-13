using System.Text.Json.Serialization;

namespace VHWuWa.Core.Models;

/// <summary>Một gói đã cài (lưu trong installed-state.json).</summary>
public sealed class InstalledPackage
{
    [JsonPropertyName("packageId")] public string PackageId { get; set; } = "";
    [JsonPropertyName("packageType")] public PackageType PackageType { get; set; }
    [JsonPropertyName("packageName")] public string PackageName { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("installedAt")] public DateTimeOffset InstalledAt { get; set; }
    [JsonPropertyName("backupId")] public string BackupId { get; set; } = "";
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    /// <summary>Danh sách đường dẫn đích (tương đối thư mục game) do gói này ghi.</summary>
    [JsonPropertyName("installedFiles")] public List<string> InstalledFiles { get; set; } = new();
}

/// <summary>Trạng thái cài đặt tổng (%LocalAppData%/VHWuWa/installed-state.json).</summary>
public sealed class InstalledState
{
    [JsonPropertyName("gamePath")] public string GamePath { get; set; } = "";
    [JsonPropertyName("installedPackages")] public List<InstalledPackage> InstalledPackages { get; set; } = new();
}

/// <summary>Một file trong bản backup.</summary>
public sealed class BackupFileEntry
{
    /// <summary>Đường dẫn đích tương đối trong game.</summary>
    [JsonPropertyName("destination")] public string Destination { get; set; } = "";
    /// <summary>File gốc có tồn tại trước khi ghi hay không (để biết khi gỡ nên khôi phục hay xóa).</summary>
    [JsonPropertyName("existedBefore")] public bool ExistedBefore { get; set; }
    [JsonPropertyName("sha256Before")] public string Sha256Before { get; set; } = "";
    [JsonPropertyName("sha256After")] public string Sha256After { get; set; } = "";
}

/// <summary>Manifest của một bản backup (Backups/&lt;id&gt;/backup-manifest.json).</summary>
public sealed class BackupManifest
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("operation")] public string Operation { get; set; } = "";
    [JsonPropertyName("packageId")] public string PackageId { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("files")] public List<BackupFileEntry> Files { get; set; } = new();
}
