using NiteChess.Stockfish.Abstractions;
using NiteChess.Stockfish;

namespace NiteChess.Desktop.Services;

public sealed class DesktopStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    private readonly StockfishAssetDownloader _downloader;

    public DesktopStockfishRuntimeBootstrapper(StockfishAssetDownloader downloader)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
    }

    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "desktop",
            IntegrationMode: StockfishIntegrationMode.NativeProcess,
            RuntimeLocation: "Assets/Stockfish/desktop-stockfish.bundle.json",
            IsBundled: true,
            Notes: "Desktop package manifest lives at Assets/Stockfish/desktop-stockfish.bundle.json and resolves the bundled Stockfish 18 executable for the current RID. Missing binaries are downloaded automatically from GitHub Releases at startup.");
    }

    public async ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var descriptor = Describe();
        var manifestPath = StockfishRuntimePathResolver.ResolveManifestPath(descriptor);
        var runtimePath = StockfishRuntimePathResolver.Resolve(descriptor);

        await _downloader.EnsureAsync(manifestPath, runtimePath, cancellationToken);
    }
}
