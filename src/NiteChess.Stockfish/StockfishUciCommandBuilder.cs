using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Stockfish;

public static class StockfishUciCommandBuilder
{
    public static IReadOnlyList<string> Build(StockfishEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PositionFen);
        ArgumentNullException.ThrowIfNull(request.SearchConfiguration);

        var config = request.SearchConfiguration;
        Validate(config);

        return new[]
        {
            "uci",
            $"setoption name Threads value {config.Threads}",
            $"setoption name Hash value {config.HashMegabytes}",
            $"setoption name MultiPV value {config.MultiPv}",
            $"setoption name Move Overhead value {config.MoveOverheadMilliseconds}",
            $"setoption name Ponder value {ToUciBoolean(config.PonderEnabled)}",
            "setoption name UCI_Chess960 value false",
            "setoption name UCI_LimitStrength value false",
            "setoption name UCI_ShowWDL value false",
            "ucinewgame",
            "isready",
            $"position fen {request.PositionFen}",
            $"go depth {config.SearchDepth}"
        };
    }

    private static void Validate(StockfishSearchConfiguration config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config.PresetId);

        if (config.SearchDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config.SearchDepth), config.SearchDepth, "Search depth must be positive.");
        }

        if (config.Threads < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config.Threads), config.Threads, "Thread count must be positive.");
        }

        if (config.HashMegabytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config.HashMegabytes), config.HashMegabytes, "Hash size must be positive.");
        }

        if (config.MultiPv < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config.MultiPv), config.MultiPv, "MultiPV must be positive.");
        }

        if (config.MoveOverheadMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config.MoveOverheadMilliseconds),
                config.MoveOverheadMilliseconds,
                "Move overhead cannot be negative.");
        }
    }

    private static string ToUciBoolean(bool value)
    {
        return value ? "true" : "false";
    }
}