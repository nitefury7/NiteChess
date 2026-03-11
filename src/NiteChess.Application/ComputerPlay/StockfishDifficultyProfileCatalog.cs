using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Application.ComputerPlay;

internal static class StockfishDifficultyProfileCatalog
{
    public static StockfishSearchConfiguration Get(AiDifficulty difficulty)
    {
        return difficulty switch
        {
            AiDifficulty.Easy => new("easy", 4, 1, 16, 1, 0, false),
            AiDifficulty.Medium => new("medium", 8, 1, 16, 1, 0, false),
            AiDifficulty.Hard => new("hard", 12, 1, 16, 1, 0, false),
            AiDifficulty.Expert => new("expert", 16, 1, 16, 1, 0, false),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported AI difficulty.")
        };
    }
}