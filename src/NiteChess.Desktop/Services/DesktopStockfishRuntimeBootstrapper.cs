using NiteChess.Stockfish.Abstractions;
using NiteChess.Stockfish;

namespace NiteChess.Desktop.Services;

public sealed class DesktopStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "desktop",
            IntegrationMode: StockfishIntegrationMode.NativeProcess,
            RuntimeLocation: "Assets/Stockfish/desktop-stockfish.bundle.json",
            IsBundled: true,
            Notes: "Desktop package manifest lives at Assets/Stockfish/desktop-stockfish.bundle.json and resolves the bundled Stockfish 18 executable for the current RID.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtimePath = StockfishRuntimePathResolver.Resolve(Describe());
        if (!File.Exists(runtimePath))
        {
            throw new InvalidOperationException($"Bundled desktop Stockfish executable was not found at '{runtimePath}'.");
        }

        return ValueTask.CompletedTask;
    }
}
