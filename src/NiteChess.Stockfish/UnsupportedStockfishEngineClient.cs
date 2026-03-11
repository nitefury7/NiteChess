using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Stockfish;

public sealed class UnsupportedStockfishEngineClient : IStockfishEngineClient
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;

    public UnsupportedStockfishEngineClient(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        _runtimeDescriptor = runtimeDescriptor ?? throw new ArgumentNullException(nameof(runtimeDescriptor));
    }

    public ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        throw new InvalidOperationException(
            $"Stockfish move requests are scaffolded but no host-specific engine bridge is bundled for '{_runtimeDescriptor.HostId}'. " +
            $"Expected local runtime path: '{_runtimeDescriptor.RuntimeLocation}'. {_runtimeDescriptor.Notes}");
    }
}