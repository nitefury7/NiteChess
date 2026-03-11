using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;

namespace NiteChess.Desktop;

public sealed class App : Avalonia.Application
{
    public static IServiceProvider? Services { get; set; }

    public App()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services?.GetRequiredService<MainWindow>()
                ?? throw new InvalidOperationException("Desktop services have not been initialized.");

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
