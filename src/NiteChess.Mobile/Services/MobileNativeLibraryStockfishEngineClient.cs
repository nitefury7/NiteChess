using System.Runtime.InteropServices;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

public sealed class MobileNativeLibraryStockfishEngineClient : IStockfishEngineClient
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;

    public MobileNativeLibraryStockfishEngineClient(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        _runtimeDescriptor = runtimeDescriptor ?? throw new ArgumentNullException(nameof(runtimeDescriptor));
    }

    public ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var commands = StockfishUciCommandBuilder.Build(request).ToArray();

        try
        {
            var bestMoveLine = MobileStockfishNativeBridge.RequestBestMove(string.Join('\n', commands));
            var (bestMoveNotation, ponderMoveNotation) = ParseBestMove(bestMoveLine);

            return ValueTask.FromResult(
                new StockfishEngineResponse(
                    bestMoveNotation,
                    ponderMoveNotation,
                    commands.Concat(new[] { bestMoveLine }).ToArray()));
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            throw new InvalidOperationException(
                $"Mobile Stockfish bridge exports were not found for runtime '{_runtimeDescriptor.RuntimeLocation}'. " +
                "Bundle the native bridge library (Android `.so` / iOS `.a`) that exposes `nitechess_stockfish_request_bestmove_utf8` and `nitechess_stockfish_free_utf8`.",
                exception);
        }
    }

    private static (string BestMoveNotation, string? PonderMoveNotation) ParseBestMove(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !string.Equals(parts[0], "bestmove", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Mobile Stockfish bridge returned malformed bestmove output '{line}'.");
        }

        var ponderMoveNotation = parts.Length >= 4 && string.Equals(parts[2], "ponder", StringComparison.Ordinal)
            ? parts[3]
            : null;

        return (parts[1], ponderMoveNotation);
    }

    private static class MobileStockfishNativeBridge
    {
        public static string RequestBestMove(string commandPayload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(commandPayload);

            var payloadPointer = Marshal.StringToCoTaskMemUTF8(commandPayload);
            try
            {
                var responsePointer = RequestBestMoveUtf8(payloadPointer);
                if (responsePointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Mobile Stockfish bridge returned a null bestmove response pointer.");
                }

                try
                {
                    return Marshal.PtrToStringUTF8(responsePointer)
                           ?? throw new InvalidOperationException("Mobile Stockfish bridge returned a null UTF-8 bestmove response.");
                }
                finally
                {
                    FreeUtf8(responsePointer);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(payloadPointer);
            }
        }

#if ANDROID
        [DllImport("nitechess_stockfish_bridge", EntryPoint = "nitechess_stockfish_request_bestmove_utf8", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr RequestBestMoveUtf8(IntPtr commandPayloadUtf8);

        [DllImport("nitechess_stockfish_bridge", EntryPoint = "nitechess_stockfish_free_utf8", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeUtf8(IntPtr value);
#elif IOS
        [DllImport("__Internal", EntryPoint = "nitechess_stockfish_request_bestmove_utf8", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr RequestBestMoveUtf8(IntPtr commandPayloadUtf8);

        [DllImport("__Internal", EntryPoint = "nitechess_stockfish_free_utf8", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeUtf8(IntPtr value);
#else
        private static IntPtr RequestBestMoveUtf8(IntPtr commandPayloadUtf8)
        {
            throw new PlatformNotSupportedException("Mobile Stockfish bridge is supported only on Android and iOS targets.");
        }

        private static void FreeUtf8(IntPtr value)
        {
        }
#endif
    }
}