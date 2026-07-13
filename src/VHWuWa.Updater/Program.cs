using System.Diagnostics;
using System.IO.Compression;
using VHWuWa.Core.Services;

namespace VHWuWa.Updater;

/// <summary>Updater riêng: đóng app chính → backup → giải nén bản mới đè lên → relaunch.
/// Rollback nếu thất bại. KHÔNG ghi đè file đang chạy (app chính đã thoát).
/// Cách gọi: VHWuWa.Updater --zip &lt;file.zip&gt; --target &lt;appDir&gt; --relaunch VHWuWa.exe [--pid 1234]</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        var o = Parse(args);
        if (!o.TryGetValue("zip", out var zip) || !o.TryGetValue("target", out var target))
        {
            Console.Error.WriteLine("Cách dùng: --zip <file.zip> --target <appDir> [--relaunch VHWuWa.exe] [--pid N]");
            return 1;
        }
        var relaunch = o.GetValueOrDefault("relaunch", "VHWuWa.exe");
        target = Path.GetFullPath(target);

        try
        {
            if (o.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var pid))
                WaitForExit(pid, TimeSpan.FromSeconds(30));

            if (!File.Exists(zip)) { Console.Error.WriteLine("Không tìm thấy file zip."); return 2; }
            Directory.CreateDirectory(target);

            var backupDir = target.TrimEnd('\\', '/') + "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Console.WriteLine("Sao lưu bản hiện tại...");
            CopyDir(target, backupDir);

            try
            {
                Console.WriteLine("Giải nén bản cập nhật...");
                ExtractOver(zip, target);
                var exe = Path.Combine(target, relaunch);
                if (!File.Exists(exe)) throw new FileNotFoundException("Thiếu file thực thi sau cập nhật: " + relaunch);

                Console.WriteLine("Cập nhật thành công. Khởi động lại...");
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = target });
                TryDelete(backupDir);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cập nhật lỗi, đang rollback: " + ex.Message);
                RestoreDir(backupDir, target);
                var exe = Path.Combine(target, relaunch);
                if (File.Exists(exe))
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = target });
                return 3;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Lỗi updater: " + ex.Message);
            return 4;
        }
    }

    private static void WaitForExit(int pid, TimeSpan timeout)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            p.WaitForExit((int)timeout.TotalMilliseconds);
        }
        catch { /* tiến trình đã thoát */ }
    }

    private static void ExtractOver(string zipPath, string target)
    {
        var rootFull = Path.GetFullPath(target);
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/')) continue; // thư mục
            if (!PathValidation.IsSafeRelativePath(name)) continue; // chống zip-slip
            var dest = PathValidation.ResolveInsideRoot(rootFull, name);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, overwrite: true);
        }
    }

    private static void CopyDir(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(src, dst));
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var t = file.Replace(src, dst);
            Directory.CreateDirectory(Path.GetDirectoryName(t)!);
            File.Copy(file, t, overwrite: true);
        }
    }

    private static void RestoreDir(string backup, string target)
    {
        if (!Directory.Exists(backup)) return;
        CopyDir(backup, target);
    }

    private static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) continue;
            var key = args[i][2..];
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : "true";
            d[key] = val;
        }
        return d;
    }
}
