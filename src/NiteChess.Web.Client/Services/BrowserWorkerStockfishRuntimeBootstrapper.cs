using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Web.Client.Services;

public sealed class BrowserWorkerStockfishRuntimeBootstrapper : IStockfishRuntimeBootstrapper
{
    public StockfishRuntimeDescriptor Describe()
    {
        return new StockfishRuntimeDescriptor(
            HostId: "web-client",
            IntegrationMode: StockfishIntegrationMode.BrowserWasmWorker,
            RuntimeLocation: "wwwroot/workers/stockfish.worker.js",
            IsBundled: false,
            Notes: "Web package manifest lives at wwwroot/stockfish/web-stockfish.bundle.json and expects Stockfish 18 WASM assets under wwwroot/stockfish for offline worker-based play.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
