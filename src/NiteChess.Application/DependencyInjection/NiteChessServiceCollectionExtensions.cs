using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NiteChess.Application.ComputerPlay;
using NiteChess.Application.Configuration;
using NiteChess.Application.GameSessions;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Application.DependencyInjection;

public static class NiteChessServiceCollectionExtensions
{
    public static IServiceCollection AddNiteChessApplication(
        this IServiceCollection services,
        NiteChessPlatformDescriptor platformDescriptor,
        StockfishRuntimeDescriptor stockfishRuntime)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(platformDescriptor);
        ArgumentNullException.ThrowIfNull(stockfishRuntime);

        services.AddSingleton(platformDescriptor);
        services.AddSingleton(stockfishRuntime);
        services.AddSingleton(new NiteChessBootstrapManifest
        {
            Platform = platformDescriptor,
            Stockfish = stockfishRuntime
        });
        services.TryAddSingleton<IStockfishEngineClient>(serviceProvider =>
            new RuntimeConfiguredStockfishEngineClient(
                serviceProvider.GetRequiredService<StockfishRuntimeDescriptor>(),
                serviceProvider.GetServices<IStockfishEngineClientFactory>()));
        services.AddSingleton<IComputerMoveService, StockfishComputerMoveService>();
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddSingleton<IGameSessionPersistenceService, GameSessionPersistenceService>();

        return services;
    }
}
