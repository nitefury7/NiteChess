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
            Notes: "Placeholder registration for future browser-side Stockfish WASM wiring.");
    }

    public ValueTask WarmUpAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
