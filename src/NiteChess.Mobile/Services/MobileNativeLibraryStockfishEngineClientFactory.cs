using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileNativeLibraryStockfishEngineClientFactory : IStockfishEngineClientFactory
{
    public bool CanCreate(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);
        return runtimeDescriptor.IntegrationMode == StockfishIntegrationMode.NativeLibrary;
    }

    public IStockfishEngineClient Create(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);
        return new MobileNativeLibraryStockfishEngineClient(runtimeDescriptor);
    }
}