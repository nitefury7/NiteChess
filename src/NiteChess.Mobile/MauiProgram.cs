using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
using NiteChess.Mobile.Services;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        builder.Services.AddNiteChessApplication(
            new NiteChessPlatformDescriptor(
                HostId: "mobile",
                Surface: ".NET MAUI",
                SupportsOfflineAi: true,
                SupportsOnlinePlay: true,
                Notes: "Single-project mobile shell targets Android and iOS."),
            new StockfishRuntimeDescriptor(
                HostId: "mobile",
                IntegrationMode: StockfishIntegrationMode.NativeLibrary,
                RuntimeLocation: "platform-local/stockfish",
                IsBundled: false,
                Notes: "Actual mobile engine packaging lands in a later wave."));
        builder.Services.AddSingleton<IStockfishRuntimeBootstrapper, MobileStockfishRuntimeBootstrapper>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
