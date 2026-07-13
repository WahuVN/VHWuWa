using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

public sealed class LogService : ILogService, IDisposable
{
    private readonly Logger _logger;
    private readonly ConcurrentQueue<LogEntry> _ring = new();
    private const int RingMax = 2000;

    // Che token/khóa/mật khẩu khỏi log
    private static readonly Regex Secret = new(
        @"(?i)(token|secret|password|apikey|api_key|authorization|private[_-]?key)\s*[:=]\s*\S+",
        RegexOptions.Compiled);

    public string LogDirectory { get; }

    public LogService(ISettingsService settings)
    {
        LogDirectory = Path.Combine(settings.AppDataDirectory, "Logs");
        Directory.CreateDirectory(LogDirectory);
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(LogDirectory, "vhwuwa-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static string Mask(string s) => Secret.Replace(s, m => m.Groups[1].Value + "=***");

    private void Push(string level, string operation, string message)
    {
        var msg = Mask($"{operation}: {message}");
        _ring.Enqueue(new LogEntry { Timestamp = DateTimeOffset.Now, Level = level, Message = msg });
        while (_ring.Count > RingMax && _ring.TryDequeue(out _)) { }
    }

    public void Info(string operation, string message)
    {
        Push("Info", operation, message);
        _logger.Information("{Op}: {Msg}", operation, Mask(message));
    }

    public void Warn(string operation, string message)
    {
        Push("Warn", operation, message);
        _logger.Warning("{Op}: {Msg}", operation, Mask(message));
    }

    public void Error(string operation, string message, Exception? ex = null)
    {
        Push("Error", operation, message);
        _logger.Error(ex, "{Op}: {Msg}", operation, Mask(message));
    }

    public IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null, string? search = null)
    {
        IEnumerable<LogEntry> q = _ring.Reverse();
        if (!string.IsNullOrWhiteSpace(levelFilter) && levelFilter != "Tất cả")
            q = q.Where(e => e.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Message.Contains(search, StringComparison.OrdinalIgnoreCase));
        return q.Take(max).ToList();
    }

    public void Clear()
    {
        while (_ring.TryDequeue(out _)) { }
        try
        {
            foreach (var f in Directory.GetFiles(LogDirectory, "*.log"))
            {
                try { File.Delete(f); } catch { }
            }
        }
        catch { }
    }

    public void Dispose() => _logger.Dispose();
}
