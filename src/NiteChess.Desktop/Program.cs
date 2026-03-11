using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
using NiteChess.Application.Gameplay;
using NiteChess.Desktop.Services;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddNiteChessApplication(
            new NiteChessPlatformDescriptor(
                HostId: "desktop",
                Surface: "AvaloniaDesktop",
                SupportsOfflineAi: true,
                SupportsOnlinePlay: true,
                Notes: "Desktop shell targets macOS and Linux with offline Stockfish packaging scaffolded under Assets/Stockfish."),
            new StockfishRuntimeDescriptor(
                HostId: "desktop",
                IntegrationMode: StockfishIntegrationMode.NativeProcess,
                RuntimeLocation: "Assets/Stockfish/desktop-stockfish.bundle.json",
                IsBundled: true,
                Notes: "Desktop runtime resolution is driven by Assets/Stockfish/desktop-stockfish.bundle.json, which ships Stockfish 18 executables in the per-RID native folders."));
        services.AddSingleton<IStockfishRuntimeBootstrapper, DesktopStockfishRuntimeBootstrapper>();
        services.AddSingleton<GameplayController>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        App.Services = services.BuildServiceProvider();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
