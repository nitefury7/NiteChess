using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Desktop.Services;

public sealed class DesktopStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "desktop",
            IntegrationMode: StockfishIntegrationMode.NativeProcess,
            RuntimeLocation: "Assets/Stockfish/native/{rid}/stockfish",
            IsBundled: false,
            Notes: "Desktop package manifest lives at Assets/Stockfish/desktop-stockfish.bundle.json and expects local Stockfish 18 executables plus NNUE files in per-RID folders.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
