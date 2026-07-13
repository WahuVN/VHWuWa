using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class BackupService : IBackupService
{
    private readonly ILogService _log;
    private readonly IHashService _hash;
    public string BackupsDirectory { get; }

    public BackupService(ISettingsService settings, ILogService log, IHashService hash)
    {
        _log = log;
        _hash = hash;
        BackupsDirectory = settings.BackupsDirectory;
        Directory.CreateDirectory(BackupsDirectory);
    }

    public BackupManifest CreateBackup(string gamePath, string operation, string packageId, string version,
        IEnumerable<string> destinations)
    {
        var id = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Guid.NewGuid().ToString("N")[..6];
        var dir = Path.Combine(BackupsDirectory, id);
        var filesDir = Path.Combine(dir, "files");
        Directory.CreateDirectory(filesDir);

        var manifest = new BackupManifest
        {
            Id = id,
            CreatedAt = DateTimeOffset.Now,
            Operation = operation,
            PackageId = packageId,
            Version = version,
        };

        foreach (var dest in destinations.Distinct())
        {
            var full = PathValidation.ResolveInsideRoot(gamePath, dest);
            var entry = new BackupFileEntry { Destination = dest };
            if (File.Exists(full))
            {
                entry.ExistedBefore = true;
                entry.Sha256Before = _hash.Sha256File(full);
                var bkpTarget = PathValidation.ResolveInsideRoot(filesDir, dest);
                Directory.CreateDirectory(Path.GetDirectoryName(bkpTarget)!);
                File.Copy(full, bkpTarget, overwrite: true);
            }
            manifest.Files.Add(entry);
        }

        File.WriteAllText(Path.Combine(dir, "backup-manifest.json"), VhwJson.Serialize(manifest));
        _log.Info("Backup", $"Đã tạo backup {id} ({manifest.Files.Count} mục).");
        return manifest;
    }

    public Result Restore(string gamePath, string backupId)
    {
        try
        {
            var dir = Path.Combine(BackupsDirectory, backupId);
            var mp = Path.Combine(dir, "backup-manifest.json");
            if (!File.Exists(mp)) return Result.Fail($"Không tìm thấy backup: {backupId}");
            var manifest = VhwJson.Deserialize<BackupManifest>(File.ReadAllText(mp));
            if (manifest is null) return Result.Fail("backup-manifest.json hỏng.");

            var filesDir = Path.Combine(dir, "files");
            foreach (var f in manifest.Files)
            {
                var full = PathValidation.ResolveInsideRoot(gamePath, f.Destination);
                if (f.ExistedBefore)
                {
                    var src = PathValidation.ResolveInsideRoot(filesDir, f.Destination);
                    if (File.Exists(src))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        File.Copy(src, full, overwrite: true);
                    }
                }
                else
                {
                    // File này do gói tạo mới -> xóa để trả về trạng thái gốc
                    if (File.Exists(full)) File.Delete(full);
                }
            }
            _log.Info("Backup", $"Đã khôi phục từ backup {backupId}.");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _log.Error("Backup", $"Khôi phục thất bại: {ex.Message}", ex);
            return Result.Fail("Khôi phục thất bại: " + ex.Message, ex);
        }
    }

    private static BackupManifest? return_fail(string _) => null;

    public IReadOnlyList<BackupInfo> List()
    {
        var list = new List<BackupInfo>();
        if (!Directory.Exists(BackupsDirectory)) return list;
        foreach (var dir in Directory.GetDirectories(BackupsDirectory))
        {
            var mp = Path.Combine(dir, "backup-manifest.json");
            if (!File.Exists(mp)) continue;
            try
            {
                var m = VhwJson.Deserialize<BackupManifest>(File.ReadAllText(mp));
                if (m is null) continue;
                list.Add(new BackupInfo
                {
                    Id = m.Id, CreatedAt = m.CreatedAt, Operation = m.Operation,
                    PackageId = m.PackageId, Version = m.Version, FileCount = m.Files.Count, Path = dir,
                });
            }
            catch { }
        }
        return list.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public Result Delete(string backupId)
    {
        try
        {
            var dir = Path.Combine(BackupsDirectory, backupId);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("Xóa backup thất bại: " + ex.Message, ex);
        }
    }
}
