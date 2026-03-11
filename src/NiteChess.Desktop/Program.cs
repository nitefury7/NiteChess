using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
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
                Notes: "Desktop shell targets macOS and Linux with future local Stockfish packaging."),
            new StockfishRuntimeDescriptor(
                HostId: "desktop",
                IntegrationMode: StockfishIntegrationMode.NativeProcess,
                RuntimeLocation: "app-local/stockfish",
                IsBundled: false,
                Notes: "Actual native binary packaging lands in a later wave."));
        services.AddSingleton<IStockfishRuntimeBootstrapper, DesktopStockfishRuntimeBootstrapper>();

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
