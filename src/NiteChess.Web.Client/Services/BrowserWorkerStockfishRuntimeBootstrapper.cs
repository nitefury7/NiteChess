using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Web.Client.Services;

public sealed class BrowserWorkerStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "web-client",
            IntegrationMode: StockfishIntegrationMode.BrowserWasmWorker,
            RuntimeLocation: "wwwroot/stockfish/web-stockfish.bundle.json",
            IsBundled: true,
            Notes: "Web package manifest lives at wwwroot/stockfish/web-stockfish.bundle.json and resolves the bundled Stockfish 18 worker/WASM assets for offline play.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
