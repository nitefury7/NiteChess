using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
using NiteChess.Application.Gameplay;
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
                Notes: "Single-project mobile shell targets Android and iOS with an offline Stockfish native bridge reserved under Resources/Raw/Stockfish."),
            new StockfishRuntimeDescriptor(
                HostId: "mobile",
                IntegrationMode: StockfishIntegrationMode.NativeLibrary,
                RuntimeLocation: "Resources/Raw/Stockfish/mobile-stockfish.bundle.json",
                IsBundled: true,
                Notes: "Bundle manifest ships at Resources/Raw/Stockfish/mobile-stockfish.bundle.json and maps Android to a bundled Stockfish executable while iOS links the bundled static bridge library."));
        builder.Services.AddSingleton<IStockfishEngineClientFactory, MobileNativeLibraryStockfishEngineClientFactory>();
        builder.Services.AddSingleton<IStockfishRuntimeBootstrapper, MobileStockfishRuntimeBootstrapper>();
        builder.Services.AddSingleton<GameplayController>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
