namespace NiteChess.Stockfish.Abstractions;

public interface IStockfishEngineClientFactory
{
    bool CanCreate(StockfishRuntimeDescriptor runtimeDescriptor);

    IStockfishEngineClient Create(StockfishRuntimeDescriptor runtimeDescriptor);
}