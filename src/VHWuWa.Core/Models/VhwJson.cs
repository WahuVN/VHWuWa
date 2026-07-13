using System.Text.Json;
using System.Text.Json.Serialization;

namespace VHWuWa.Core.Models;

/// <summary>Tùy chọn JSON dùng chung (camelCase, enum dạng chuỗi, bỏ qua null).</summary>
public static class VhwJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return o;
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}

/// <summary>Tiến trình cài đặt (dùng với IProgress).</summary>
public readonly record struct InstallProgress(int Percent, string CurrentFile, int Completed, int Total);
