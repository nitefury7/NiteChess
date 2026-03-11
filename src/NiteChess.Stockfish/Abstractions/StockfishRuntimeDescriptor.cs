namespace NiteChess.Stockfish.Abstractions;

public sealed record StockfishRuntimeDescriptor(
    string HostId,
    StockfishIntegrationMode IntegrationMode,
    string RuntimeLocation,
    bool IsBundled,
    string Notes);
