using System.Text;
using System.Text.Json;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class GraphicsService : IGraphicsService
{
    private readonly IBackupService _backup;
    private readonly ILogService _log;
    public GraphicsConfig Config { get; }

    public GraphicsService(IBackupService backup, ILogService log, string? configDir = null)
    {
        _backup = backup; _log = log;
        var path = Path.Combine(configDir ?? AppPaths.DefaultConfigDir, "graphics.json");
        try
        {
            Config = File.Exists(path)
                ? VhwJson.Deserialize<GraphicsConfig>(File.ReadAllText(path)) ?? new GraphicsConfig()
                : new GraphicsConfig();
        }
        catch { Config = new GraphicsConfig(); }
    }

    public bool IsSupported => !string.IsNullOrWhiteSpace(Config.ConfigPath) && Config.Options.Count > 0;

    public string? ConfigFilePath(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(Config.ConfigPath)) return null;
        try { return PathValidation.ResolveInsideRoot(gamePath, Config.ConfigPath); }
        catch { return null; }
    }

    public Dictionary<string, string> ReadCurrent(string gamePath)
    {
        var result = new Dictionary<string, string>();
        var file = ConfigFilePath(gamePath);
        if (file is null || !File.Exists(file)) return result;
        try
        {
            if (Config.ConfigFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                foreach (var opt in Config.Options)
                    if (doc.RootElement.TryGetProperty(opt.Key, out var el))
                        result[opt.Key] = el.ToString();
            }
            else
            {
                var ini = ParseIni(File.ReadAllText(file));
                foreach (var opt in Config.Options)
                    if (ini.TryGetValue(Key(opt.Section, opt.Key), out var v)) result[opt.Key] = v;
            }
        }
        catch (Exception ex) { _log.Error("Graphics", $"Đọc cấu hình lỗi: {ex.Message}", ex); }
        return result;
    }

    public Result Apply(string gamePath, Dictionary<string, string> values)
    {
        if (!IsSupported) return Result.Fail("Tính năng này chưa được cấu hình cho game hiện tại.");
        var file = ConfigFilePath(gamePath);
        if (file is null) return Result.Fail("Không xác định được file cấu hình.");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            // Backup file cấu hình
            _backup.CreateBackup(gamePath, "graphics", "graphics-config", "", new[] { Config.ConfigPath });

            if (Config.ConfigFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                WriteJson(file, values);
            else
                WriteIni(file, values);

            _log.Info("Graphics", $"Đã áp {values.Count} thiết lập đồ họa.");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _log.Error("Graphics", $"Ghi cấu hình lỗi: {ex.Message}", ex);
            return Result.Fail("Ghi cấu hình lỗi: " + ex.Message, ex);
        }
    }

    public Result ApplyPreset(string gamePath, string presetName)
    {
        var preset = Config.Presets.FirstOrDefault(p =>
            p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase));
        if (preset is null) return Result.Fail("Không tìm thấy preset: " + presetName);
        return Apply(gamePath, new Dictionary<string, string>(preset.Values));
    }

    // ---- INI helpers ----
    private static string Key(string section, string key) => $"{section}\u0001{key}";

    private static Dictionary<string, string> ParseIni(string text)
    {
        var map = new Dictionary<string, string>();
        string section = "";
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1].Trim(); continue; }
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            map[Key(section, line[..eq].Trim())] = line[(eq + 1)..].Trim();
        }
        return map;
    }

    private void WriteIni(string file, Dictionary<string, string> values)
    {
        var lines = File.Exists(file) ? File.ReadAllLines(file).ToList() : new List<string>();
        foreach (var opt in Config.Options)
        {
            if (!values.TryGetValue(opt.Key, out var val)) continue;
            UpsertIni(lines, opt.Section, opt.Key, val);
        }
        File.WriteAllLines(file, lines, new UTF8Encoding(false));
    }

    private static void UpsertIni(List<string> lines, string section, string key, string val)
    {
        int secStart = -1, secEnd = lines.Count;
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].Trim();
            if (t.StartsWith('[') && t.EndsWith(']'))
            {
                var name = t[1..^1].Trim();
                if (secStart >= 0) { secEnd = i; break; }
                if (name.Equals(section, StringComparison.OrdinalIgnoreCase)) { secStart = i; secEnd = lines.Count; }
            }
        }
        if (secStart < 0)
        {
            if (lines.Count > 0 && lines[^1].Trim().Length != 0) lines.Add("");
            lines.Add($"[{section}]");
            lines.Add($"{key}={val}");
            return;
        }
        for (int i = secStart + 1; i < secEnd; i++)
        {
            var t = lines[i].Trim();
            var eq = t.IndexOf('=');
            if (eq > 0 && t[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"{key}={val}";
                return;
            }
        }
        lines.Insert(secEnd, $"{key}={val}");
    }

    private static void WriteJson(string file, Dictionary<string, string> values)
    {
        Dictionary<string, object?> obj;
        try
        {
            obj = File.Exists(file)
                ? JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(file)) ?? new()
                : new();
        }
        catch { obj = new(); }
        foreach (var kv in values) obj[kv.Key] = kv.Value;
        File.WriteAllText(file, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
    }
}
