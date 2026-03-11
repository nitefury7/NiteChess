namespace NiteChess.Stockfish.Abstractions;

public sealed record StockfishEngineResponse(
    string BestMoveNotation,
    string? PonderMoveNotation,
    IReadOnlyList<string> Commands);