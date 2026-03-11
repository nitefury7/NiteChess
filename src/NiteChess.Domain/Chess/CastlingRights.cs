namespace NiteChess.Domain.Chess;

public enum CastlingSide
{
    KingSide = 0,
    QueenSide = 1
}

public readonly record struct CastlingRights(bool WhiteKingSide, bool WhiteQueenSide, bool BlackKingSide, bool BlackQueenSide)
{
    public static CastlingRights All => new(true, true, true, true);

    public static CastlingRights None => new(false, false, false, false);

    public bool CanCastle(ChessColor color, CastlingSide side)
    {
        return (color, side) switch
        {
            (ChessColor.White, CastlingSide.KingSide) => WhiteKingSide,
            (ChessColor.White, CastlingSide.QueenSide) => WhiteQueenSide,
            (ChessColor.Black, CastlingSide.KingSide) => BlackKingSide,
            (ChessColor.Black, CastlingSide.QueenSide) => BlackQueenSide,
            _ => false
        };
    }

    public CastlingRights WithoutColor(ChessColor color)
    {
        return color == ChessColor.White
            ? this with { WhiteKingSide = false, WhiteQueenSide = false }
            : this with { BlackKingSide = false, BlackQueenSide = false };
    }

    public CastlingRights WithoutSide(ChessColor color, CastlingSide side)
    {
        return (color, side) switch
        {
            (ChessColor.White, CastlingSide.KingSide) => this with { WhiteKingSide = false },
            (ChessColor.White, CastlingSide.QueenSide) => this with { WhiteQueenSide = false },
            (ChessColor.Black, CastlingSide.KingSide) => this with { BlackKingSide = false },
            (ChessColor.Black, CastlingSide.QueenSide) => this with { BlackQueenSide = false },
            _ => this
        };
    }
}