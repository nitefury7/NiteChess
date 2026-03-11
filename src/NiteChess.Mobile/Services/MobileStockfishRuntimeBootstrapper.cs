using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "mobile",
            IntegrationMode: StockfishIntegrationMode.NativeLibrary,
            RuntimeLocation: "Resources/Raw/Stockfish/mobile-stockfish.bundle.json",
            IsBundled: true,
            Notes: "Mobile package manifest lives at Resources/Raw/Stockfish/mobile-stockfish.bundle.json and maps Android/iOS to their bundled Stockfish runtime assets.");
    }

    public async ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        await MobileBundledStockfishRuntime.WarmUpAsync(Describe(), cancellationToken);
    }
}
