using System.Reflection;
using System.Text.Json;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class UpdateService : IUpdateService
{
    private const string ReleasesApi = "https://api.github.com/repos/WahuVN/VHWuWa/releases/latest";
    private static readonly HttpClient Http = CreateClient();
    private readonly ILogService _log;
    private readonly IHashService _hash;
    private readonly string _currentVersion;

    public UpdateService(ILogService log, IHashService hash, string? currentVersion = null)
    {
        _log = log; _hash = hash;
        _currentVersion = currentVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("VHWuWa-Updater/1.0");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var res = new UpdateCheckResult { CurrentVersion = _currentVersion };
        try
        {
            using var resp = await Http.GetAsync(ReleasesApi, ct);
            if (!resp.IsSuccessStatusCode)
            {
                res.Message = $"Không kiểm tra được cập nhật (HTTP {(int)resp.StatusCode}).";
                return res;
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            var version = tag.TrimStart('v', 'V');
            if (string.IsNullOrWhiteSpace(version)) { res.Message = "Chưa có bản phát hành."; return res; }

            string zipUrl = "", updateJsonUrl = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";
                    if (name.Equals("update.json", StringComparison.OrdinalIgnoreCase)) updateJsonUrl = url;
                    else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && zipUrl == "") zipUrl = url;
                }
            }

            UpdateManifest manifest;
            if (!string.IsNullOrEmpty(updateJsonUrl))
            {
                var mjson = await Http.GetStringAsync(updateJsonUrl, ct);
                manifest = VhwJson.Deserialize<UpdateManifest>(mjson) ?? new UpdateManifest();
                if (string.IsNullOrWhiteSpace(manifest.Version)) manifest.Version = version;
            }
            else
            {
                manifest = new UpdateManifest { Version = version, ReleaseNotes = notes, DownloadUrl = zipUrl };
            }

            res.Manifest = manifest;
            res.UpdateAvailable = VersionComparer.IsNewer(manifest.Version, _currentVersion);
            res.Message = res.UpdateAvailable
                ? $"Có bản mới {manifest.Version}."
                : "Bạn đang dùng phiên bản mới nhất.";
            return res;
        }
        catch (Exception ex)
        {
            _log.Warn("Update", $"Kiểm tra cập nhật lỗi: {ex.Message}");
            res.Message = "Không thể kiểm tra cập nhật: " + ex.Message;
            return res;
        }
    }

    public async Task<Result<string>> DownloadAsync(UpdateManifest manifest, string destDir,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
                return Result<string>.Fail("Không có đường dẫn tải.");
            Directory.CreateDirectory(destDir);
            var fileName = Path.GetFileName(new Uri(manifest.DownloadUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = $"VHWuWa-{manifest.Version}.zip";
            var dest = Path.Combine(destDir, fileName);

            using var resp = await Http.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1;
            await using (var input = await resp.Content.ReadAsStreamAsync(ct))
            await using (var output = File.Create(dest))
            {
                var buffer = new byte[81920];
                long read = 0; int n;
                while ((n = await input.ReadAsync(buffer, ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, n), ct);
                    read += n;
                    if (total > 0) progress?.Report(read * 100.0 / total);
                }
            }

            if (!string.IsNullOrWhiteSpace(manifest.Sha256) && !_hash.Verify(dest, manifest.Sha256))
            {
                File.Delete(dest);
                return Result<string>.Fail("SHA-256 bản tải không khớp — đã hủy.");
            }
            return Result<string>.Ok(dest);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Tải cập nhật lỗi: " + ex.Message, ex);
        }
    }
}
