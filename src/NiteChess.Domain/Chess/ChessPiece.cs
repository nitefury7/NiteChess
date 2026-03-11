namespace NiteChess.Domain.Chess;

public readonly record struct ChessPiece(ChessColor Color, PieceType Type)
{
    public override string ToString()
    {
        return $"{Color} {Type}";
    }
}