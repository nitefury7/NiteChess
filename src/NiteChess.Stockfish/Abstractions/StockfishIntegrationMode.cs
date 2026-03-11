namespace NiteChess.Stockfish.Abstractions;

public enum StockfishIntegrationMode
{
    Disabled = 0,
    NativeProcess = 1,
    NativeLibrary = 2,
    BrowserWasmWorker = 3
}
