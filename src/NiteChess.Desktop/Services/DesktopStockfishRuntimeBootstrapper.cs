using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Desktop.Services;

public sealed class DesktopStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "desktop",
            IntegrationMode: StockfishIntegrationMode.NativeProcess,
            RuntimeLocation: "app-local/stockfish",
            IsBundled: false,
            Notes: "Desktop scaffold reserves a local native Stockfish seam.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
