using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileNativeLibraryStockfishEngineClientFactory : IStockfishEngineClientFactory
{
    public bool CanCreate(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);
        return runtimeDescriptor.IntegrationMode == StockfishIntegrationMode.NativeLibrary
               && (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
    }

    public IStockfishEngineClient Create(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);

        if (OperatingSystem.IsAndroid())
        {
            return new MobileAndroidProcessStockfishEngineClient(runtimeDescriptor);
        }

        if (OperatingSystem.IsIOS())
        {
            return new MobileNativeLibraryStockfishEngineClient(runtimeDescriptor);
        }

        throw new PlatformNotSupportedException("Mobile Stockfish runtimes are only supported on Android and iOS targets.");
    }
}