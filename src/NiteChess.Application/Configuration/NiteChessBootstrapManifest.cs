using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Application.Configuration;

public sealed class NiteChessBootstrapManifest
{
    public required NiteChessPlatformDescriptor Platform { get; init; }

    public required StockfishRuntimeDescriptor Stockfish { get; init; }
}
