using System.Text;
using NiteChess.Domain.Chess;

namespace NiteChess.Stockfish;

public static class StockfishFenSerializer
{
    public static string ToFen(ChessGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return string.Join(
            ' ',
            SerializeBoard(game.Board),
            game.SideToMove == ChessColor.White ? "w" : "b",
            SerializeCastlingRights(game.CastlingRights),
            game.EnPassantTarget?.ToString() ?? "-",
            game.HalfmoveClock.ToString(),
            game.FullmoveNumber.ToString());
    }

    private static string SerializeBoard(ChessBoard board)
    {
        var ranks = new string[8];

        for (var rank = 7; rank >= 0; rank--)
        {
            var builder = new StringBuilder();
            var emptySquares = 0;

            for (var file = 0; file < 8; file++)
            {
                var piece = board[new ChessPosition(file, rank)];

                if (piece is null)
                {
                    emptySquares++;
                    continue;
                }

                if (emptySquares > 0)
                {
                    builder.Append(emptySquares);
                    emptySquares = 0;
                }

                builder.Append(ToFenPiece(piece.Value));
            }

            if (emptySquares > 0)
            {
                builder.Append(emptySquares);
            }

            ranks[7 - rank] = builder.ToString();
        }

        return string.Join('/', ranks);
    }

    private static string SerializeCastlingRights(CastlingRights castlingRights)
    {
        var builder = new StringBuilder();

        if (castlingRights.WhiteKingSide)
        {
            builder.Append('K');
        }

        if (castlingRights.WhiteQueenSide)
        {
            builder.Append('Q');
        }

        if (castlingRights.BlackKingSide)
        {
            builder.Append('k');
        }

        if (castlingRights.BlackQueenSide)
        {
            builder.Append('q');
        }

        return builder.Length == 0 ? "-" : builder.ToString();
    }

    private static char ToFenPiece(ChessPiece piece)
    {
        var fen = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece.Type, "Unsupported piece type for FEN serialization.")
        };

        return piece.Color == ChessColor.White ? char.ToUpperInvariant(fen) : fen;
    }
}