using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileAndroidProcessStockfishEngineClient : IStockfishEngineClient
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;

    public MobileAndroidProcessStockfishEngineClient(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        _runtimeDescriptor = runtimeDescriptor ?? throw new ArgumentNullException(nameof(runtimeDescriptor));
    }

    public async ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executablePath = await MobileBundledStockfishRuntime.ResolveAndroidExecutablePathAsync(
            _runtimeDescriptor,
            cancellationToken);

        var processDescriptor = new StockfishRuntimeDescriptor(
            _runtimeDescriptor.HostId,
            StockfishIntegrationMode.NativeProcess,
            executablePath,
            isBundled: true,
            Notes: $"Android Stockfish process extracted from bundle manifest '{_runtimeDescriptor.RuntimeLocation}'.");

        var processClient = new NativeProcessStockfishEngineClient(processDescriptor);
        return await processClient.GetBestMoveAsync(request, cancellationToken);
    }
}