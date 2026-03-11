namespace NiteChess.Domain.Chess;

public sealed class ChessGame
{
    private static readonly (int FileDelta, int RankDelta)[] KnightOffsets =
    {
        (-2, -1), (-2, 1), (-1, -2), (-1, 2),
        (1, -2), (1, 2), (2, -1), (2, 1)
    };

    private static readonly (int FileDelta, int RankDelta)[] KingOffsets =
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),           (0, 1),
        (1, -1),  (1, 0),  (1, 1)
    };

    private static readonly (int FileDelta, int RankDelta)[] BishopDirections =
    {
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    private static readonly (int FileDelta, int RankDelta)[] RookDirections =
    {
        (-1, 0), (1, 0), (0, -1), (0, 1)
    };

    private static readonly PieceType[] PromotionPieceTypes =
    {
        PieceType.Queen,
        PieceType.Rook,
        PieceType.Bishop,
        PieceType.Knight
    };

    public ChessGame(
        ChessBoard board,
        ChessColor sideToMove,
        CastlingRights castlingRights,
        ChessPosition? enPassantTarget = null,
        int halfmoveClock = 0,
        int fullmoveNumber = 1)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));

        if (halfmoveClock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(halfmoveClock), halfmoveClock, "Halfmove clock cannot be negative.");
        }

        if (fullmoveNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fullmoveNumber), fullmoveNumber, "Fullmove number must be at least 1.");
        }

        SideToMove = sideToMove;
        CastlingRights = castlingRights;
        EnPassantTarget = enPassantTarget;
        HalfmoveClock = halfmoveClock;
        FullmoveNumber = fullmoveNumber;
    }

    public ChessBoard Board { get; }

    public ChessColor SideToMove { get; }

    public CastlingRights CastlingRights { get; }

    public ChessPosition? EnPassantTarget { get; }

    public int HalfmoveClock { get; }

    public int FullmoveNumber { get; }

    public static ChessGame CreateInitial()
    {
        return new ChessGame(ChessBoard.CreateInitial(), ChessColor.White, CastlingRights.All);
    }

    public IReadOnlyList<ChessMove> GetLegalMoves()
    {
        return GenerateLegalMoves();
    }

    public IReadOnlyList<ChessMove> GetLegalMoves(ChessPosition from)
    {
        return GenerateLegalMoves()
            .Where(move => move.From == from)
            .ToArray();
    }

    public bool IsLegalMove(ChessMove move)
    {
        return GenerateLegalMoves().Contains(move);
    }

    public bool TryApplyMove(ChessMove move, out ChessGame? nextGame)
    {
        if (!IsLegalMove(move))
        {
            nextGame = null;
            return false;
        }

        nextGame = ApplyPseudoLegalMove(move);
        return true;
    }

    public ChessGame ApplyMove(ChessMove move)
    {
        if (!TryApplyMove(move, out var nextGame) || nextGame is null)
        {
            throw new InvalidOperationException($"Move '{move}' is not legal in the current position.");
        }

        return nextGame;
    }

    public bool IsInCheck(ChessColor color)
    {
        var kingPosition = FindKing(Board, color);
        return IsSquareAttacked(Board, kingPosition, color.Opponent());
    }

    public ChessGameStatus GetStatus()
    {
        var legalMoves = GenerateLegalMoves();
        var isCurrentPlayerInCheck = IsInCheck(SideToMove);

        if (legalMoves.Count == 0)
        {
            return isCurrentPlayerInCheck ? ChessGameStatus.Checkmate : ChessGameStatus.Stalemate;
        }

        return isCurrentPlayerInCheck ? ChessGameStatus.Check : ChessGameStatus.InProgress;
    }

    private List<ChessMove> GenerateLegalMoves()
    {
        var legalMoves = new List<ChessMove>();

        foreach (var square in Board.GetOccupiedSquares())
        {
            if (square.Piece.Color != SideToMove)
            {
                continue;
            }

            foreach (var move in GeneratePseudoLegalMoves(square.Position, square.Piece))
            {
                var candidate = ApplyPseudoLegalMove(move);

                if (!candidate.IsInCheck(SideToMove))
                {
                    legalMoves.Add(move);
                }
            }
        }

        return legalMoves;
    }

    private IEnumerable<ChessMove> GeneratePseudoLegalMoves(ChessPosition from, ChessPiece piece)
    {
        return piece.Type switch
        {
            PieceType.Pawn => GeneratePawnMoves(from, piece),
            PieceType.Knight => GenerateJumpMoves(from, piece, KnightOffsets),
            PieceType.Bishop => GenerateSlidingMoves(from, piece, BishopDirections),
            PieceType.Rook => GenerateSlidingMoves(from, piece, RookDirections),
            PieceType.Queen => GenerateSlidingMoves(from, piece, BishopDirections).Concat(GenerateSlidingMoves(from, piece, RookDirections)),
            PieceType.King => GenerateKingMoves(from, piece),
            _ => Enumerable.Empty<ChessMove>()
        };
    }

    private IEnumerable<ChessMove> GeneratePawnMoves(ChessPosition from, ChessPiece piece)
    {
        var moves = new List<ChessMove>();
        var forward = piece.Color == ChessColor.White ? 1 : -1;
        var startRank = piece.Color == ChessColor.White ? 1 : 6;
        var promotionRank = piece.Color == ChessColor.White ? 7 : 0;

        if (from.TryOffset(0, forward, out var oneAhead) && Board.IsEmpty(oneAhead))
        {
            AddPawnMoves(moves, from, oneAhead, promotionRank);

            if (from.Rank == startRank &&
                from.TryOffset(0, forward * 2, out var twoAhead) &&
                Board.IsEmpty(twoAhead))
            {
                moves.Add(new ChessMove(from, twoAhead));
            }
        }

        foreach (var fileDelta in new[] { -1, 1 })
        {
            if (!from.TryOffset(fileDelta, forward, out var target))
            {
                continue;
            }

            var targetPiece = Board[target];

            if (targetPiece is ChessPiece occupiedPiece && CanCapturePiece(piece.Color, occupiedPiece))
            {
                AddPawnMoves(moves, from, target, promotionRank);
                continue;
            }

            if (EnPassantTarget is ChessPosition enPassantTarget && enPassantTarget == target)
            {
                moves.Add(new ChessMove(from, target));
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GenerateJumpMoves(ChessPosition from, ChessPiece piece, IReadOnlyList<(int FileDelta, int RankDelta)> offsets)
    {
        var moves = new List<ChessMove>();

        foreach (var (fileDelta, rankDelta) in offsets)
        {
            if (!from.TryOffset(fileDelta, rankDelta, out var target))
            {
                continue;
            }

            var targetPiece = Board[target];

            if (targetPiece is null || CanCapturePiece(piece.Color, targetPiece.Value))
            {
                moves.Add(new ChessMove(from, target));
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GenerateSlidingMoves(ChessPosition from, ChessPiece piece, IReadOnlyList<(int FileDelta, int RankDelta)> directions)
    {
        var moves = new List<ChessMove>();

        foreach (var (fileDelta, rankDelta) in directions)
        {
            var current = from;

            while (current.TryOffset(fileDelta, rankDelta, out var target))
            {
                var targetPiece = Board[target];

                if (targetPiece is null)
                {
                    moves.Add(new ChessMove(from, target));
                    current = target;
                    continue;
                }

                if (CanCapturePiece(piece.Color, targetPiece.Value))
                {
                    moves.Add(new ChessMove(from, target));
                }

                break;
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GenerateKingMoves(ChessPosition from, ChessPiece piece)
    {
        var moves = new List<ChessMove>();

        moves.AddRange(GenerateJumpMoves(from, piece, KingOffsets));

        if (CanCastle(piece.Color, CastlingSide.KingSide))
        {
            moves.Add(new ChessMove(from, new ChessPosition(6, GetHomeRank(piece.Color))));
        }

        if (CanCastle(piece.Color, CastlingSide.QueenSide))
        {
            moves.Add(new ChessMove(from, new ChessPosition(2, GetHomeRank(piece.Color))));
        }

        return moves;
    }

    private void AddPawnMoves(List<ChessMove> moves, ChessPosition from, ChessPosition to, int promotionRank)
    {
        if (to.Rank == promotionRank)
        {
            foreach (var promotionPieceType in PromotionPieceTypes)
            {
                moves.Add(new ChessMove(from, to, promotionPieceType));
            }

            return;
        }

        moves.Add(new ChessMove(from, to));
    }

    private static bool CanCapturePiece(ChessColor movingColor, ChessPiece targetPiece)
    {
        return targetPiece.Color != movingColor && targetPiece.Type != PieceType.King;
    }

    private bool CanCastle(ChessColor color, CastlingSide side)
    {
        if (!CastlingRights.CanCastle(color, side))
        {
            return false;
        }

        var homeRank = GetHomeRank(color);
        var kingStart = new ChessPosition(4, homeRank);
        var rookStart = side == CastlingSide.KingSide ? new ChessPosition(7, homeRank) : new ChessPosition(0, homeRank);
        var king = Board[kingStart];
        var rook = Board[rookStart];

        if (king is not ChessPiece kingPiece || kingPiece.Color != color || kingPiece.Type != PieceType.King)
        {
            return false;
        }

        if (rook is not ChessPiece rookPiece || rookPiece.Color != color || rookPiece.Type != PieceType.Rook)
        {
            return false;
        }

        var emptySquares = side == CastlingSide.KingSide
            ? new[] { new ChessPosition(5, homeRank), new ChessPosition(6, homeRank) }
            : new[] { new ChessPosition(1, homeRank), new ChessPosition(2, homeRank), new ChessPosition(3, homeRank) };

        if (emptySquares.Any(square => !Board.IsEmpty(square)))
        {
            return false;
        }

        var transitSquares = side == CastlingSide.KingSide
            ? new[] { kingStart, new ChessPosition(5, homeRank), new ChessPosition(6, homeRank) }
            : new[] { kingStart, new ChessPosition(3, homeRank), new ChessPosition(2, homeRank) };

        return transitSquares.All(square => !IsSquareAttacked(Board, square, color.Opponent()));
    }

    private ChessGame ApplyPseudoLegalMove(ChessMove move)
    {
        var movingPiece = Board[move.From] ?? throw new InvalidOperationException($"No piece exists at '{move.From}'.");
        var board = Board.Clone();
        var capturedPiece = board[move.To];
        var isEnPassantCapture = movingPiece.Type == PieceType.Pawn &&
                                 EnPassantTarget is ChessPosition enPassantTarget &&
                                 enPassantTarget == move.To &&
                                 capturedPiece is null &&
                                 move.From.File != move.To.File;

        if (isEnPassantCapture)
        {
            var capturedPawnPosition = new ChessPosition(move.To.File, move.From.Rank);
            capturedPiece = board[capturedPawnPosition];
            board.SetPiece(capturedPawnPosition, null);
        }

        board.SetPiece(move.From, null);

        if (movingPiece.Type == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2)
        {
            MoveCastlingRook(board, movingPiece.Color, move.To.File > move.From.File ? CastlingSide.KingSide : CastlingSide.QueenSide);
        }

        var placedPiece = move.PromotionPieceType is PieceType promotionPieceType
            ? new ChessPiece(movingPiece.Color, promotionPieceType)
            : movingPiece;

        board.SetPiece(move.To, placedPiece);

        var nextCastlingRights = UpdateCastlingRights(movingPiece, move.From, move.To, capturedPiece);
        ChessPosition? nextEnPassantTarget = movingPiece.Type == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2
            ? new ChessPosition(move.From.File, (move.From.Rank + move.To.Rank) / 2)
            : null;
        var nextHalfmoveClock = movingPiece.Type == PieceType.Pawn || capturedPiece is not null ? 0 : HalfmoveClock + 1;
        var nextFullmoveNumber = SideToMove == ChessColor.Black ? FullmoveNumber + 1 : FullmoveNumber;

        return new ChessGame(
            board,
            SideToMove.Opponent(),
            nextCastlingRights,
            nextEnPassantTarget,
            nextHalfmoveClock,
            nextFullmoveNumber);
    }

    private void MoveCastlingRook(ChessBoard board, ChessColor color, CastlingSide side)
    {
        var homeRank = GetHomeRank(color);
        var rookFrom = side == CastlingSide.KingSide ? new ChessPosition(7, homeRank) : new ChessPosition(0, homeRank);
        var rookTo = side == CastlingSide.KingSide ? new ChessPosition(5, homeRank) : new ChessPosition(3, homeRank);
        var rook = board[rookFrom] ?? throw new InvalidOperationException("Cannot castle without the rook on its starting square.");
        board.SetPiece(rookFrom, null);
        board.SetPiece(rookTo, rook);
    }

    private CastlingRights UpdateCastlingRights(ChessPiece movingPiece, ChessPosition from, ChessPosition to, ChessPiece? capturedPiece)
    {
        var rights = CastlingRights;

        if (movingPiece.Type == PieceType.King)
        {
            rights = rights.WithoutColor(movingPiece.Color);
        }

        if (movingPiece.Type == PieceType.Rook)
        {
            rights = RemoveRookCastlingRight(rights, movingPiece.Color, from);
        }

        if (capturedPiece is ChessPiece captured && captured.Type == PieceType.Rook)
        {
            rights = RemoveRookCastlingRight(rights, captured.Color, to);
        }

        return rights;
    }

    private CastlingRights RemoveRookCastlingRight(CastlingRights rights, ChessColor rookColor, ChessPosition rookSquare)
    {
        var homeRank = GetHomeRank(rookColor);

        if (rookSquare == new ChessPosition(0, homeRank))
        {
            return rights.WithoutSide(rookColor, CastlingSide.QueenSide);
        }

        if (rookSquare == new ChessPosition(7, homeRank))
        {
            return rights.WithoutSide(rookColor, CastlingSide.KingSide);
        }

        return rights;
    }

    private static ChessPosition FindKing(ChessBoard board, ChessColor color)
    {
        foreach (var square in board.GetOccupiedSquares())
        {
            if (square.Piece.Color == color && square.Piece.Type == PieceType.King)
            {
                return square.Position;
            }
        }

        throw new InvalidOperationException($"Board does not contain a {color} king.");
    }

    private static bool IsSquareAttacked(ChessBoard board, ChessPosition target, ChessColor attackerColor)
    {
        var pawnRankDelta = attackerColor == ChessColor.White ? -1 : 1;

        foreach (var fileDelta in new[] { -1, 1 })
        {
            if (target.TryOffset(fileDelta, pawnRankDelta, out var pawnSource) &&
                board[pawnSource] is ChessPiece pawn &&
                pawn.Color == attackerColor &&
                pawn.Type == PieceType.Pawn)
            {
                return true;
            }
        }

        foreach (var (fileDelta, rankDelta) in KnightOffsets)
        {
            if (target.TryOffset(fileDelta, rankDelta, out var source) &&
                board[source] is ChessPiece knight &&
                knight.Color == attackerColor &&
                knight.Type == PieceType.Knight)
            {
                return true;
            }
        }

        foreach (var (fileDelta, rankDelta) in KingOffsets)
        {
            if (target.TryOffset(fileDelta, rankDelta, out var source) &&
                board[source] is ChessPiece king &&
                king.Color == attackerColor &&
                king.Type == PieceType.King)
            {
                return true;
            }
        }

        return IsAttackedAlongDirections(board, target, attackerColor, BishopDirections, PieceType.Bishop, PieceType.Queen) ||
               IsAttackedAlongDirections(board, target, attackerColor, RookDirections, PieceType.Rook, PieceType.Queen);
    }

    private static bool IsAttackedAlongDirections(
        ChessBoard board,
        ChessPosition target,
        ChessColor attackerColor,
        IReadOnlyList<(int FileDelta, int RankDelta)> directions,
        PieceType primaryType,
        PieceType secondaryType)
    {
        foreach (var (fileDelta, rankDelta) in directions)
        {
            var current = target;

            while (current.TryOffset(fileDelta, rankDelta, out var source))
            {
                var piece = board[source];

                if (piece is null)
                {
                    current = source;
                    continue;
                }

                return piece.Value.Color == attackerColor &&
                       (piece.Value.Type == primaryType || piece.Value.Type == secondaryType);
            }
        }

        return false;
    }

    private static int GetHomeRank(ChessColor color)
    {
        return color == ChessColor.White ? 0 : 7;
    }
}