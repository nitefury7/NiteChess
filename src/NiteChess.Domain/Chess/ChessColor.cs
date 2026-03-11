namespace NiteChess.Domain.Chess;

public enum ChessColor
{
    White = 0,
    Black = 1
}

internal static class ChessColorExtensions
{
    public static ChessColor Opponent(this ChessColor color)
    {
        return color == ChessColor.White ? ChessColor.Black : ChessColor.White;
    }
}