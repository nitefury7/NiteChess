namespace NiteChess.Stockfish.Abstractions;

public sealed record StockfishSearchConfiguration(
    string PresetId,
    int SearchDepth,
    int Threads,
    int HashMegabytes,
    int MultiPv,
    int MoveOverheadMilliseconds,
    bool PonderEnabled);