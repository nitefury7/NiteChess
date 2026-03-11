using Microsoft.Extensions.DependencyInjection;
using NiteChess.Application.Configuration;
using NiteChess.Application.GameSessions;
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
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddSingleton<IGameSessionPersistenceService, GameSessionPersistenceService>();

        return services;
    }
}
