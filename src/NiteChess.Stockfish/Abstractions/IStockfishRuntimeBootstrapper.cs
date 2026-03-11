namespace NiteChess.Stockfish.Abstractions;

public interface IStockfishRuntimeBootstrapper
{
    StockfishRuntimeDescriptor Describe();

    ValueTask WarmUpAsync(CancellationToken cancellationToken = default);
}
