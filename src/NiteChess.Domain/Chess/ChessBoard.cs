namespace NiteChess.Domain.Chess;

public sealed class ChessBoard
{
    private readonly ChessPiece?[] _squares;

    public ChessBoard()
        : this(new ChessPiece?[64])
    {
    }

    private ChessBoard(ChessPiece?[] squares)
    {
        if (squares.Length != 64)
        {
            throw new ArgumentException("Board storage must contain exactly 64 squares.", nameof(squares));
        }

        _squares = squares;
    }

    public ChessPiece? this[ChessPosition position] => _squares[position.ToIndex()];

    public static ChessBoard CreateEmpty()
    {
        return new ChessBoard();
    }

    public static ChessBoard CreateInitial()
    {
        var board = CreateEmpty();
        PlaceBackRank(board, ChessColor.White, 0);
        PlacePawns(board, ChessColor.White, 1);
        PlacePawns(board, ChessColor.Black, 6);
        PlaceBackRank(board, ChessColor.Black, 7);
        return board;
    }

    public IEnumerable<(ChessPosition Position, ChessPiece Piece)> GetOccupiedSquares()
    {
        for (var index = 0; index < _squares.Length; index++)
        {
            if (_squares[index] is ChessPiece piece)
            {
                yield return (ChessPosition.FromIndex(index), piece);
            }
        }
    }

    public bool IsEmpty(ChessPosition position)
    {
        return this[position] is null;
    }

    public ChessBoard WithPiece(ChessPosition position, ChessPiece? piece)
    {
        var clone = Clone();
        clone.SetPiece(position, piece);
        return clone;
    }

    internal ChessBoard Clone()
    {
        return new ChessBoard((ChessPiece?[])_squares.Clone());
    }

    internal void SetPiece(ChessPosition position, ChessPiece? piece)
    {
        _squares[position.ToIndex()] = piece;
    }

    private static void PlaceBackRank(ChessBoard board, ChessColor color, int rank)
    {
        var pieces = new[]
        {
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Queen,
            PieceType.King,
            PieceType.Bishop,
            PieceType.Knight,
            PieceType.Rook
        };

        for (var file = 0; file < pieces.Length; file++)
        {
            board.SetPiece(new ChessPosition(file, rank), new ChessPiece(color, pieces[file]));
        }
    }

    private static void PlacePawns(ChessBoard board, ChessColor color, int rank)
    {
        for (var file = 0; file < 8; file++)
        {
            board.SetPiece(new ChessPosition(file, rank), new ChessPiece(color, PieceType.Pawn));
        }
    }
}