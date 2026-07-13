using System.Text.Json.Serialization;

namespace VHWuWa.Core.Models;

/// <summary>Cấu hình nhận diện game (Config/game.json).</summary>
public sealed class GameConfig
{
    [JsonPropertyName("gameId")] public string GameId { get; set; } = "";
    [JsonPropertyName("gameName")] public string GameName { get; set; } = "";
    [JsonPropertyName("executable")] public string Executable { get; set; } = "";
    [JsonPropertyName("requiredFiles")] public List<string> RequiredFiles { get; set; } = new();
    [JsonPropertyName("possibleRegistryKeys")] public List<string> PossibleRegistryKeys { get; set; } = new();
    [JsonPropertyName("steamAppId")] public string SteamAppId { get; set; } = "";
}

/// <summary>Một tùy chọn đồ họa có nhiều mức.</summary>
public sealed class GraphicsOption
{
    [JsonPropertyName("key")] public string Key { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    /// <summary>section trong file ini (nếu định dạng ini). Rỗng nếu json.</summary>
    [JsonPropertyName("section")] public string Section { get; set; } = "";
    [JsonPropertyName("choices")] public List<string> Choices { get; set; } = new();
}

/// <summary>Một preset đồ họa: map key -> value.</summary>
public sealed class GraphicsPreset
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("values")] public Dictionary<string, string> Values { get; set; } = new();
}

/// <summary>Cấu hình đồ họa cho game (Config/graphics.json). Không hard-code theo game.</summary>
public sealed class GraphicsConfig
{
    [JsonPropertyName("configFormat")] public string ConfigFormat { get; set; } = "ini"; // ini | json
    /// <summary>Đường dẫn file cấu hình TƯƠNG ĐỐI so với thư mục game.</summary>
    [JsonPropertyName("configPath")] public string ConfigPath { get; set; } = "";
    [JsonPropertyName("options")] public List<GraphicsOption> Options { get; set; } = new();
    [JsonPropertyName("presets")] public List<GraphicsPreset> Presets { get; set; } = new();
}

/// <summary>Manifest cập nhật ứng dụng (từ GitHub Releases).</summary>
public sealed class UpdateManifest
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("minimumVersion")] public string MinimumVersion { get; set; } = "";
    [JsonPropertyName("releaseNotes")] public string ReleaseNotes { get; set; } = "";
    [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; } = "";
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("mandatory")] public bool Mandatory { get; set; }
}
