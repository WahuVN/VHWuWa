using System.Text.Json.Serialization;

namespace VHWuWa.Core.Models;

/// <summary>Một file bên trong gói .vhwpack.</summary>
public sealed class PackageFileEntry
{
    /// <summary>Đường dẫn nguồn trong gói (thường bắt đầu bằng "payload/").</summary>
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    /// <summary>Đường dẫn đích TƯƠNG ĐỐI so với thư mục game.</summary>
    [JsonPropertyName("destination")] public string Destination { get; set; } = "";
    /// <summary>SHA-256 (hex thường) của nội dung file.</summary>
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";
    [JsonPropertyName("operation")] public FileOperation Operation { get; set; } = FileOperation.Replace;
}

/// <summary>Manifest của một gói .vhwpack (Việt hóa / mod / font).</summary>
public sealed class PackageManifest
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = 1;
    [JsonPropertyName("packageId")] public string PackageId { get; set; } = "";
    [JsonPropertyName("packageName")] public string PackageName { get; set; } = "";
    [JsonPropertyName("packageType")] public PackageType PackageType { get; set; } = PackageType.Translation;
    [JsonPropertyName("author")] public string Author { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0.0";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("supportedGameVersions")] public List<string> SupportedGameVersions { get; set; } = new();
    [JsonPropertyName("files")] public List<PackageFileEntry> Files { get; set; } = new();
}
