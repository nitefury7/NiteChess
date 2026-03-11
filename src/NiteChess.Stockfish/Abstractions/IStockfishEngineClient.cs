namespace NiteChess.Stockfish.Abstractions;

public interface IStockfishEngineClient
{
    ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default);
}