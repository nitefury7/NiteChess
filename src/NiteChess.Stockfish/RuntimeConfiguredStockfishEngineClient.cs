using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Stockfish;

public sealed class RuntimeConfiguredStockfishEngineClient : IStockfishEngineClient
{
    private readonly IStockfishEngineClient _innerClient;

    public RuntimeConfiguredStockfishEngineClient(StockfishRuntimeDescriptor runtimeDescriptor)
        : this(runtimeDescriptor, Array.Empty<IStockfishEngineClientFactory>())
    {
    }

    public RuntimeConfiguredStockfishEngineClient(
        StockfishRuntimeDescriptor runtimeDescriptor,
        IEnumerable<IStockfishEngineClientFactory> clientFactories)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);
        ArgumentNullException.ThrowIfNull(clientFactories);

        _innerClient = runtimeDescriptor.IntegrationMode switch
        {
            StockfishIntegrationMode.NativeProcess => new NativeProcessStockfishEngineClient(runtimeDescriptor),
            StockfishIntegrationMode.NativeLibrary => ResolveHostClient(runtimeDescriptor, clientFactories),
            _ => new UnsupportedStockfishEngineClient(runtimeDescriptor)
        };
    }

    public ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        return _innerClient.GetBestMoveAsync(request, cancellationToken);
    }

    private static IStockfishEngineClient ResolveHostClient(
        StockfishRuntimeDescriptor runtimeDescriptor,
        IEnumerable<IStockfishEngineClientFactory> clientFactories)
    {
        foreach (var clientFactory in clientFactories)
        {
            if (clientFactory.CanCreate(runtimeDescriptor))
            {
                return clientFactory.Create(runtimeDescriptor);
            }
        }

        return new UnsupportedStockfishEngineClient(runtimeDescriptor);
    }
}