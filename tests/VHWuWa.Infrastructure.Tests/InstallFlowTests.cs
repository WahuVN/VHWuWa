using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class InstallFlowTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "vhw_inf_" + Guid.NewGuid().ToString("N"));
    private readonly string _game;
    private readonly string _config;
    private readonly string _appData;
    private readonly HashService _hash = new();
    private readonly RsaSignatureService _sig = new();

    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly GameDetectionService _detect;
    private readonly BackupService _backup;
    private readonly PackageInstallerService _installer;

    public InstallFlowTests()
    {
        _game = Path.Combine(_work, "game");
        _config = Path.Combine(_work, "Config");
        _appData = Path.Combine(_work, "appdata");
        Directory.CreateDirectory(Path.Combine(_game, "Data"));
        Directory.CreateDirectory(_config);

        File.WriteAllText(Path.Combine(_game, "Game.exe"), "exe");
        File.WriteAllText(Path.Combine(_game, "Data", "text.pak"), "ORIGINAL");

        File.WriteAllText(Path.Combine(_config, "game.json"), VhwJson.Serialize(new GameConfig
        {
            GameId = "demo", GameName = "Demo Game", Executable = "Game.exe",
            RequiredFiles = { "Game.exe" }
        }));

        _settings = new SettingsService(_appData);
        _log = new LogService(_settings);
        _detect = new GameDetectionService(_log, _config);
        _backup = new BackupService(_settings, _log, _hash);
        _installer = new PackageInstallerService(_detect, _backup, _settings, _hash, _sig, _log, _config);
    }

    private string MakePack(string packageId, PackageType type, string dest, string content, FileOperation op)
    {
        var src = Path.Combine(_work, "src_" + packageId);
        Directory.CreateDirectory(Path.Combine(src, "payload"));
        File.WriteAllText(Path.Combine(src, "payload", "file.bin"), content);
        var m = new PackageManifest
        {
            PackageId = packageId, PackageName = packageId, PackageType = type, Version = "1.0.0",
            Files = { new PackageFileEntry { Source = "payload/file.bin", Destination = dest, Operation = op } }
        };
        File.WriteAllText(Path.Combine(src, "manifest.json"), VhwJson.Serialize(m));
        var outFile = Path.Combine(_work, packageId + ".vhwpack");
        VhwPackageWriter.Create(src, outFile, _hash);
        return outFile;
    }

    [Fact]
    public void GameDetection_Validates_Required_Files()
    {
        Assert.True(_detect.Validate(_game).IsValid);
        Assert.False(_detect.Validate(Path.Combine(_work, "nope")).IsValid);
    }

    [Fact]
    public async Task Install_Then_Uninstall_Restores_Original()
    {
        var pack = MakePack("vietnamese-pack", PackageType.Translation, "Data/text.pak", "VIETHOA", FileOperation.Replace);

        var install = await _installer.InstallAsync(_game, pack);
        Assert.True(install.Success, install.Error);
        Assert.Equal("VIETHOA", File.ReadAllText(Path.Combine(_game, "Data", "text.pak")));

        var state = _settings.LoadState();
        Assert.Single(state.InstalledPackages);
        Assert.False(string.IsNullOrEmpty(state.InstalledPackages[0].BackupId));

        var uninstall = await _installer.UninstallAsync(_game, "vietnamese-pack");
        Assert.True(uninstall.Success, uninstall.Error);
        Assert.Equal("ORIGINAL", File.ReadAllText(Path.Combine(_game, "Data", "text.pak")));
        Assert.Empty(_settings.LoadState().InstalledPackages);
    }

    [Fact]
    public async Task Install_New_File_Then_Uninstall_Removes_It()
    {
        var pack = MakePack("mod-a", PackageType.Mod, "Data/mod.pak", "MODDATA", FileOperation.Copy);
        var newFile = Path.Combine(_game, "Data", "mod.pak");

        Assert.True((await _installer.InstallAsync(_game, pack)).Success);
        Assert.True(File.Exists(newFile));

        Assert.True((await _installer.UninstallAsync(_game, "mod-a")).Success);
        Assert.False(File.Exists(newFile)); // file mới do gói tạo -> bị xóa khi gỡ
    }

    [Fact]
    public async Task Mod_Conflict_Is_Detected()
    {
        var a = MakePack("mod-a", PackageType.Mod, "Data/shared.pak", "A", FileOperation.Copy);
        await _installer.InstallAsync(_game, a);

        var b = MakePack("mod-b", PackageType.Mod, "Data/shared.pak", "B", FileOperation.Copy);
        var mod = new ModService(_settings, _backup, _log);
        var conflicts = mod.DetectConflicts(b);
        Assert.NotEmpty(conflicts);
    }

    [Fact]
    public async Task Uninstall_Without_Backup_Refuses()
    {
        var pack = MakePack("p", PackageType.Mod, "Data/x.pak", "X", FileOperation.Copy);
        await _installer.InstallAsync(_game, pack);
        // Xóa backup để mô phỏng mất backup
        foreach (var d in Directory.GetDirectories(_backup.BackupsDirectory)) Directory.Delete(d, true);
        var r = await _installer.UninstallAsync(_game, "p");
        Assert.False(r.Success);
        Assert.Contains("backup", r.Error!, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _log.Dispose();
        try { if (Directory.Exists(_work)) Directory.Delete(_work, true); } catch { }
    }
}
