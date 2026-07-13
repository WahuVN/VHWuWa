using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using VHWuWa.App.ViewModels;
using VHWuWa.App.Views;
using VHWuWa.Core.Abstractions;
using VHWuWa.Infrastructure;
using Wpf.Ui.Appearance;

namespace VHWuWa.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        services.AddVhwInfrastructure(configDir);

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<InstallViewModel>();
        services.AddSingleton<ModViewModel>();
        services.AddSingleton<FontViewModel>();
        services.AddSingleton<GraphicsViewModel>();
        services.AddSingleton<BackupViewModel>();
        services.AddSingleton<GuideViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LogViewModel>();

        // Pages
        services.AddSingleton<HomePage>();
        services.AddSingleton<InstallPage>();
        services.AddSingleton<ModPage>();
        services.AddSingleton<FontPage>();
        services.AddSingleton<GraphicsPage>();
        services.AddSingleton<BackupPage>();
        services.AddSingleton<GuidePage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<LogPage>();

        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        // Bắt lỗi UI chưa xử lý (không crash im lặng)
        DispatcherUnhandledException += OnUnhandledException;

        ApplyTheme(Services.GetRequiredService<ISettingsService>().Settings.Theme);
        Services.GetRequiredService<MainWindow>().Show();
    }

    public static void ApplyTheme(string theme)
    {
        var t = theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;
        ApplicationThemeManager.Apply(t);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show("Đã xảy ra lỗi không mong muốn:\n" + e.Exception.Message,
            "VHWuWa", MessageBoxButton.OK, MessageBoxImage.Error);
        try { Services.GetService<ILogService>()?.Error("UI", e.Exception.Message, e.Exception); } catch { }
        e.Handled = true;
    }
}
