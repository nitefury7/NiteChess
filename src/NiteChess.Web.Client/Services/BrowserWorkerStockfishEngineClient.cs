using Microsoft.JSInterop;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Web.Client.Services;

public sealed class BrowserWorkerStockfishEngineClient : IStockfishEngineClient, IAsyncDisposable
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public BrowserWorkerStockfishEngineClient(IJSRuntime jsRuntime, StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);

        _runtimeDescriptor = runtimeDescriptor ?? throw new ArgumentNullException(nameof(runtimeDescriptor));
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", "./stockfish/stockfishInterop.js").AsTask());
    }

    public async ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var commands = StockfishUciCommandBuilder.Build(request).ToArray();
        var module = await _moduleTask.Value;
        var result = await module.InvokeAsync<BrowserWorkerAnalysisResult>(
            "requestBestMove",
            cancellationToken,
            NormalizeRuntimeLocation(_runtimeDescriptor.RuntimeLocation),
            commands);

        return new StockfishEngineResponse(
            result.BestMoveNotation,
            result.PonderMoveNotation,
            result.Transcript ?? Array.Empty<string>());
    }

    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated)
        {
            return;
        }

        var module = await _moduleTask.Value;
        await module.DisposeAsync();
    }

    private static string NormalizeRuntimeLocation(string runtimeLocation)
    {
        return runtimeLocation.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
            ? runtimeLocation["wwwroot/".Length..]
            : runtimeLocation;
    }

    private sealed class BrowserWorkerAnalysisResult
    {
        public string BestMoveNotation { get; set; } = string.Empty;

        public string? PonderMoveNotation { get; set; }

        public string[]? Transcript { get; set; }
    }
}