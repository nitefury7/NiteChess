namespace NiteChess.Domain.Chess;

public readonly record struct ChessMove(ChessPosition From, ChessPosition To, PieceType? PromotionPieceType = null)
{
    public static ChessMove Parse(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation) || (notation.Length != 4 && notation.Length != 5))
        {
            throw new ArgumentException("Move notation must be in coordinate form like e2e4 or e7e8q.", nameof(notation));
        }

        var from = ChessPosition.Parse(notation[..2]);
        var to = ChessPosition.Parse(notation.Substring(2, 2));
        PieceType? promotionPieceType = null;

        if (notation.Length == 5)
        {
            promotionPieceType = char.ToLowerInvariant(notation[4]) switch
            {
                'q' => PieceType.Queen,
                'r' => PieceType.Rook,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                _ => throw new ArgumentException($"'{notation[4]}' is not a valid promotion designator.", nameof(notation))
            };
        }

        return new ChessMove(from, to, promotionPieceType);
    }

    public override string ToString()
    {
        return PromotionPieceType is null
            ? $"{From}{To}"
            : $"{From}{To}{ToPromotionCharacter(PromotionPieceType.Value)}";
    }

    private static char ToPromotionCharacter(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Queen => 'q',
            PieceType.Rook => 'r',
            PieceType.Bishop => 'b',
            PieceType.Knight => 'n',
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, "Only queen, rook, bishop, or knight are valid promotion targets.")
        };
    }
}