namespace NiteChess.Stockfish.Abstractions;

public sealed record StockfishEngineRequest(
    string PositionFen,
    StockfishSearchConfiguration SearchConfiguration);