using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

public sealed class GameDetectionService : IGameDetectionService
{
    private readonly ILogService _log;
    public GameConfig GameConfig { get; }

    public GameDetectionService(ILogService log, string? configDir = null)
    {
        _log = log;
        var dir = configDir ?? AppPaths.DefaultConfigDir;
        var path = Path.Combine(dir, "game.json");
        try
        {
            GameConfig = File.Exists(path)
                ? VhwJson.Deserialize<GameConfig>(File.ReadAllText(path)) ?? new GameConfig()
                : new GameConfig();
        }
        catch (Exception ex)
        {
            _log.Error("GameDetection", $"Không đọc được game.json: {ex.Message}", ex);
            GameConfig = new GameConfig();
        }
    }

    public GameValidation Validate(string gamePath)
    {
        var r = new GameValidation { GamePath = gamePath };
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            r.Message = "Thư mục game không tồn tại.";
            return r;
        }
        if (GameConfig.RequiredFiles.Count == 0)
        {
            r.Message = "Chưa cấu hình danh sách file bắt buộc (Config/game.json).";
            return r;
        }
        foreach (var rf in GameConfig.RequiredFiles)
        {
            var full = Path.Combine(gamePath, rf.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) r.MissingFiles.Add(rf);
        }
        r.IsValid = r.MissingFiles.Count == 0;
        r.DetectedVersion = r.IsValid ? DetectVersion(gamePath) : null;
        r.Message = r.IsValid
            ? "Đường dẫn game hợp lệ."
            : "Không tìm thấy file " + string.Join(", ", r.MissingFiles);
        return r;
    }

    public string? DetectVersion(string gamePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(GameConfig.Executable))
            {
                var exe = Path.Combine(gamePath, GameConfig.Executable);
                if (File.Exists(exe))
                {
                    var v = FileVersionInfo.GetVersionInfo(exe).FileVersion;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            // fallback: version.txt / *.version
            var vf = Path.Combine(gamePath, "version.txt");
            if (File.Exists(vf)) return File.ReadAllText(vf).Trim();
        }
        catch { }
        return null;
    }

    public IReadOnlyList<string> AutoDetect()
    {
        var found = new List<string>();
        void TryAdd(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            if (found.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) return;
            if (Directory.Exists(p) && Validate(p).IsValid) found.Add(p);
        }

        // 1) Registry (Windows)
        if (OperatingSystem.IsWindows())
        {
            foreach (var key in GameConfig.PossibleRegistryKeys)
                TryAdd(ReadRegistryInstallLocation(key));
        }

        // 2) Steam libraries: <lib>/steamapps/common/*
        foreach (var lib in SteamLibraries())
        {
            var common = Path.Combine(lib, "steamapps", "common");
            if (!Directory.Exists(common)) continue;
            foreach (var sub in SafeDirs(common))
                TryAdd(sub);
        }

        // 3) Vị trí phổ biến trên các ổ đĩa
        var names = new[]
        {
            GameConfig.GameName, GameConfig.GameId,
            "Wuthering Waves Game", "Wuthering Waves"
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (var name in names)
            {
                TryAdd(Path.Combine(drive.RootDirectory.FullName, name));
                TryAdd(Path.Combine(drive.RootDirectory.FullName, "Games", name));
                TryAdd(Path.Combine(drive.RootDirectory.FullName, "Program Files", name));
            }
        }
        return found;
    }

    [SupportedOSPlatform("windows")]
    private static string ReadRegistryInstallLocation(string keyPath)
    {
        try
        {
            // keyPath dạng HKLM\SOFTWARE\... ; đọc value InstallLocation
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                var sub = keyPath;
                if (sub.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)) sub = sub[5..];
                else if (sub.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase)) sub = sub[5..];
                using var k = root.OpenSubKey(sub);
                var loc = k?.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(loc)) return loc!;
            }
        }
        catch { }
        return "";
    }

    private static IEnumerable<string> SteamLibraries()
    {
        var libs = new List<string>();
        string? steam = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
                steam = k?.GetValue("SteamPath") as string;
            }
            catch { }
        }
        steam ??= @"C:\Program Files (x86)\Steam";
        if (!Directory.Exists(steam)) return libs;
        libs.Add(steam);
        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            try
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\""))
                {
                    var p = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(p)) libs.Add(p);
                }
            }
            catch { }
        }
        return libs.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SafeDirs(string root)
    {
        try { return Directory.GetDirectories(root); }
        catch { return Array.Empty<string>(); }
    }
}
