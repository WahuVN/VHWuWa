using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class PackageInstallerService : IPackageInstallerService
{
    private readonly IGameDetectionService _game;
    private readonly IBackupService _backup;
    private readonly ISettingsService _settings;
    private readonly IHashService _hash;
    private readonly ISignatureService _sig;
    private readonly ILogService _log;
    private readonly string _publicKeyPem;
    private readonly string _modCacheDir;

    public PackageInstallerService(IGameDetectionService game, IBackupService backup,
        ISettingsService settings, IHashService hash, ISignatureService sig, ILogService log,
        string? configDir = null)
    {
        _game = game; _backup = backup; _settings = settings; _hash = hash; _sig = sig; _log = log;
        _modCacheDir = Path.Combine(settings.AppDataDirectory, "ModCache");
        var keyPath = Path.Combine(configDir ?? AppPaths.DefaultConfigDir, "public_key.pem");
        _publicKeyPem = File.Exists(keyPath) ? File.ReadAllText(keyPath) : "";
    }

    public async Task<Result<PackageManifest>> InspectAsync(string vhwpackPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(vhwpackPath)) return Result<PackageManifest>.Fail("Không tìm thấy gói.");
            using var reader = VhwPackageReader.Open(vhwpackPath);
            var v = reader.ValidateStructure();
            if (!v.Success) return Result<PackageManifest>.Fail(v.Error!);
            if (!string.IsNullOrEmpty(_publicKeyPem) && !reader.VerifySignature(_sig, _publicKeyPem))
                return Result<PackageManifest>.Fail("Chữ ký gói không hợp lệ.");
            var h = await reader.VerifyPayloadHashesAsync(_hash, ct);
            if (!h.Success) return Result<PackageManifest>.Fail(h.Error!);
            return Result<PackageManifest>.Ok(reader.Manifest);
        }
        catch (Exception ex)
        {
            return Result<PackageManifest>.Fail("Đọc gói thất bại: " + ex.Message, ex);
        }
    }

    public async Task<Result> InstallAsync(string gamePath, string vhwpackPath,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        BackupManifest? backup = null;
        PackageManifest? manifest = null;
        try
        {
            var val = _game.Validate(gamePath);
            if (!val.IsValid) return Result.Fail(val.Message);
            if (!File.Exists(vhwpackPath)) return Result.Fail("Không tìm thấy gói .vhwpack.");

            using var reader = VhwPackageReader.Open(vhwpackPath);
            var structure = reader.ValidateStructure();
            if (!structure.Success) return Result.Fail(structure.Error!);
            manifest = reader.Manifest;

            if (!string.IsNullOrEmpty(_publicKeyPem))
            {
                if (!reader.VerifySignature(_sig, _publicKeyPem))
                    return Result.Fail("Chữ ký gói không hợp lệ — từ chối cài để đảm bảo an toàn.");
            }
            else _log.Warn("Install", "Chưa cấu hình public key — bỏ qua kiểm tra chữ ký.");

            var hashCheck = await reader.VerifyPayloadHashesAsync(_hash, ct);
            if (!hashCheck.Success) return Result.Fail(hashCheck.Error!);

            // Kiểm phiên bản game hỗ trợ (cảnh báo, không chặn)
            if (manifest.SupportedGameVersions.Count > 0 && !string.IsNullOrWhiteSpace(val.DetectedVersion)
                && !manifest.SupportedGameVersions.Contains(val.DetectedVersion!))
                _log.Warn("Install", $"Gói không liệt kê phiên bản game {val.DetectedVersion} — vẫn tiếp tục.");

            // Kiểm dung lượng trống + quyền ghi
            var need = manifest.Files.Sum(f => reader.OpenPayloadLength(f.Source));
            if (!HasFreeSpace(gamePath, need + 64L * 1024 * 1024))
                return Result.Fail("Không đủ dung lượng trống trên ổ đĩa game.");
            if (!CanWrite(gamePath))
                return Result.Fail("Không có quyền ghi vào thư mục game. Hãy đóng game hoặc chạy lại.");

            // Backup
            var destinations = manifest.Files.Select(f => f.Destination).ToList();
            backup = _backup.CreateBackup(gamePath, "install:" + manifest.PackageType, manifest.PackageId,
                manifest.Version, destinations);

            // Cài từng file
            var cacheDir = Path.Combine(_modCacheDir, manifest.PackageId);
            if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
            int total = manifest.Files.Count, done = 0;
            foreach (var f in manifest.Files)
            {
                ct.ThrowIfCancellationRequested();
                var dest = PathValidation.ResolveInsideRoot(gamePath, f.Destination);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                await using (var src = reader.OpenPayload(f.Source))
                await using (var outFs = File.Create(dest))
                    await src.CopyToAsync(outFs, ct);

                // cache để bật/tắt sau này
                var cache = PathValidation.ResolveInsideRoot(cacheDir, f.Destination);
                Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
                File.Copy(dest, cache, overwrite: true);

                done++;
                progress?.Report(new InstallProgress(total == 0 ? 100 : done * 100 / total,
                    f.Destination, done, total));
            }

            // Verify sau cài
            foreach (var f in manifest.Files)
            {
                if (string.IsNullOrWhiteSpace(f.Sha256)) continue;
                var dest = PathValidation.ResolveInsideRoot(gamePath, f.Destination);
                if (!_hash.Verify(dest, f.Sha256))
                    throw new InvalidOperationException($"File sau cài sai hash: {f.Destination}");
            }

            // Lưu trạng thái
            var state = _settings.LoadState();
            state.GamePath = gamePath;
            state.InstalledPackages.RemoveAll(p => p.PackageId == manifest.PackageId);
            state.InstalledPackages.Add(new InstalledPackage
            {
                PackageId = manifest.PackageId,
                PackageType = manifest.PackageType,
                PackageName = manifest.PackageName,
                Version = manifest.Version,
                InstalledAt = DateTimeOffset.Now,
                BackupId = backup.Id,
                Enabled = true,
                InstalledFiles = destinations,
            });
            _settings.SaveState(state);

            _log.Info("Install", $"Đã cài '{manifest.PackageName}' v{manifest.Version} ({total} file).");
            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            if (backup is not null) _backup.Restore(gamePath, backup.Id);
            _log.Warn("Install", "Người dùng đã hủy — đã rollback.");
            return Result.Fail("Đã hủy cài đặt và khôi phục file gốc.");
        }
        catch (Exception ex)
        {
            if (backup is not null) _backup.Restore(gamePath, backup.Id);
            _log.Error("Install", $"Cài thất bại: {ex.Message}", ex);
            return Result.Fail("Cài thất bại (đã rollback): " + ex.Message, ex);
        }
    }

    public Task<Result> UninstallAsync(string gamePath, string packageId, CancellationToken ct = default)
    {
        try
        {
            var state = _settings.LoadState();
            var pkg = state.InstalledPackages.FirstOrDefault(p => p.PackageId == packageId);
            if (pkg is null) return Task.FromResult(Result.Fail("Gói chưa được cài."));

            if (string.IsNullOrWhiteSpace(pkg.BackupId)
                || !Directory.Exists(Path.Combine(_backup.BackupsDirectory, pkg.BackupId)))
            {
                return Task.FromResult(Result.Fail(
                    "Không tìm thấy backup của gói này. Không tự ý xóa file để tránh hỏng game. " +
                    "Hãy dùng Steam → Xác minh file, hoặc trình quản lý game để kiểm tra."));
            }

            var restore = _backup.Restore(gamePath, pkg.BackupId);
            if (!restore.Success) return Task.FromResult(restore);

            // Dọn các thư mục rỗng do gói tạo (VD ~mods, wuwaVietHoa)
            RemoveEmptyDirsUpward(gamePath, pkg.InstalledFiles);

            var cache = Path.Combine(_modCacheDir, packageId);
            if (Directory.Exists(cache)) Directory.Delete(cache, true);

            state.InstalledPackages.RemoveAll(p => p.PackageId == packageId);
            _settings.SaveState(state);
            _log.Info("Uninstall", $"Đã gỡ gói '{packageId}'.");
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            _log.Error("Uninstall", $"Gỡ thất bại: {ex.Message}", ex);
            return Task.FromResult(Result.Fail("Gỡ thất bại: " + ex.Message, ex));
        }
    }

    private static void RemoveEmptyDirsUpward(string gamePath, IEnumerable<string> destinations)
    {
        var root = Path.GetFullPath(gamePath);
        foreach (var d in destinations)
        {
            try
            {
                var dir = Path.GetDirectoryName(PathValidation.ResolveInsideRoot(gamePath, d));
                while (!string.IsNullOrEmpty(dir)
                       && dir.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(dir, root, StringComparison.OrdinalIgnoreCase)
                       && Directory.Exists(dir)
                       && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch { }
        }
    }

    private static bool HasFreeSpace(string gamePath, long need)    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(gamePath));
            if (string.IsNullOrEmpty(root)) return true;
            return new DriveInfo(root).AvailableFreeSpace >= need;
        }
        catch { return true; }
    }

    private static bool CanWrite(string gamePath)
    {
        try
        {
            var probe = Path.Combine(gamePath, ".vhwuwa_write_test");
            File.WriteAllText(probe, "x");
            File.Delete(probe);
            return true;
        }
        catch { return false; }
    }
}
