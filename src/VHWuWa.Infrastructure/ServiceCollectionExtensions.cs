using Microsoft.Extensions.DependencyInjection;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Đăng ký toàn bộ service hạ tầng của VHWuWa.</summary>
    public static IServiceCollection AddVhwInfrastructure(this IServiceCollection services,
        string? configDir = null, string? appDataDir = null)
    {
        services.AddSingleton<ISettingsService>(_ => new SettingsService(appDataDir));
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IHashService, HashService>();
        services.AddSingleton<ISignatureService, RsaSignatureService>();

        services.AddSingleton<IGameDetectionService>(sp =>
            new GameDetectionService(sp.GetRequiredService<ILogService>(), configDir));
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPackageInstallerService>(sp => new PackageInstallerService(
            sp.GetRequiredService<IGameDetectionService>(),
            sp.GetRequiredService<IBackupService>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IHashService>(),
            sp.GetRequiredService<ISignatureService>(),
            sp.GetRequiredService<ILogService>(),
            configDir));
        services.AddSingleton<IModService, ModService>();
        services.AddSingleton<IFontService, FontService>();
        services.AddSingleton<IGraphicsService>(sp => new GraphicsService(
            sp.GetRequiredService<IBackupService>(), sp.GetRequiredService<ILogService>(), configDir));
        services.AddSingleton<IUpdateService>(sp => new UpdateService(
            sp.GetRequiredService<ILogService>(), sp.GetRequiredService<IHashService>()));
        return services;
    }
}
