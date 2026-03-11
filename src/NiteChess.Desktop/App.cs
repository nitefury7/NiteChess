using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using NiteChess.Application.Configuration;

namespace NiteChess.Desktop;

public sealed class App : Application
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
            var manifest = Services?.GetRequiredService<NiteChessBootstrapManifest>()
                ?? throw new InvalidOperationException("Desktop services have not been initialized.");

            desktop.MainWindow = new MainWindow(new MainWindowViewModel(manifest));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
