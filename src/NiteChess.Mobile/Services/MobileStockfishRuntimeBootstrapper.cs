using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "mobile",
            IntegrationMode: StockfishIntegrationMode.NativeLibrary,
            RuntimeLocation: "Resources/Raw/Stockfish/native/{platform}/libnitechess_stockfish_bridge",
            IsBundled: false,
            Notes: "Mobile package manifest lives at Resources/Raw/Stockfish/mobile-stockfish.bundle.json and expects a native bridge library exposing the NiteChess Stockfish C ABI plus linked Stockfish 18 assets in per-platform folders.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
