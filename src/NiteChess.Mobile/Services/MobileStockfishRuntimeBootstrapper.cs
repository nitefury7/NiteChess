using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "mobile",
            IntegrationMode: StockfishIntegrationMode.NativeLibrary,
            RuntimeLocation: "platform-local/stockfish",
            IsBundled: false,
            Notes: "Mobile scaffold reserves a native library seam for local Stockfish.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
