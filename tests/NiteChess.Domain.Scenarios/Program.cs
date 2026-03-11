using NiteChess.Domain.Chess;

var scenarios = new (string Name, Action Execute)[]
{
    ("Initial position offers 20 legal moves", InitialPositionOffersTwentyLegalMoves),
    ("Fool's mate is checkmate", FoolsMateIsCheckmate),
    ("Custom positions never generate direct king captures", CustomPositionDoesNotGenerateDirectKingCapture),
    ("Pinned rook cannot abandon its file", PinnedRookCannotExposeItsKing),
    ("Castling is available when squares are clear and safe", CastlingIsGeneratedWhenLegal),
    ("Castling through check is rejected", CastlingThroughCheckIsRejected),
    ("En passant is available immediately and removes the captured pawn", EnPassantIsAvailableImmediately),
    ("En passant expires after an intervening turn", EnPassantExpiresAfterAnInterveningTurn),
    ("Promotion generates four target pieces", PromotionGeneratesAllStandardChoices),
    ("Known king and queen layout is stalemate", KnownPositionIsStalemate)
};

var failures = new List<string>();

foreach (var (name, execute) in scenarios)
{
    try
    {
        execute();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
        Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"{failures.Count} scenario(s) failed.");

    foreach (var failure in failures)
    {
        Console.Error.WriteLine($" - {failure}");
    }

    return 1;
}

Console.WriteLine();
Console.WriteLine($"All {scenarios.Length} scenario(s) passed.");
return 0;

static void InitialPositionOffersTwentyLegalMoves()
{
    var game = ChessGame.CreateInitial();

    Assert.Equal(20, game.GetLegalMoves().Count, "Expected the standard opening move count.");
    Assert.Equal(ChessGameStatus.InProgress, game.GetStatus(), "The opening position should be in progress.");
    Assert.False(game.IsInCheck(ChessColor.White), "White should not start in check.");
    Assert.False(game.IsInCheck(ChessColor.Black), "Black should not start in check.");
}

static void FoolsMateIsCheckmate()
{
    var game = ChessGame.CreateInitial()
        .ApplyMove(ChessMove.Parse("f2f3"))
        .ApplyMove(ChessMove.Parse("e7e5"))
        .ApplyMove(ChessMove.Parse("g2g4"))
        .ApplyMove(ChessMove.Parse("d8h4"));

    Assert.Equal(ChessGameStatus.Checkmate, game.GetStatus(), "Fool's mate should end the game by checkmate.");
    Assert.True(game.IsInCheck(ChessColor.White), "White should be in check after Qh4#.");
}

static void PinnedRookCannotExposeItsKing()
{
    var game = CreateGame(
        ChessColor.White,
        CastlingRights.None,
        ("e1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("e2", new ChessPiece(ChessColor.White, PieceType.Rook)),
        ("a8", new ChessPiece(ChessColor.Black, PieceType.King)),
        ("e8", new ChessPiece(ChessColor.Black, PieceType.Rook)));

    Assert.False(game.IsLegalMove(ChessMove.Parse("e2d2")), "Pinned rook should not be able to leave the e-file.");
    Assert.True(game.IsLegalMove(ChessMove.Parse("e2e8")), "Capturing the attacking rook should remain legal.");
}

static void CustomPositionDoesNotGenerateDirectKingCapture()
{
    var customGame = CreateGame(
        ChessColor.White,
        CastlingRights.None,
        ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("e7", new ChessPiece(ChessColor.White, PieceType.Queen)),
        ("e8", new ChessPiece(ChessColor.Black, PieceType.King)));

    var illegalCapture = ChessMove.Parse("e7e8");
    Assert.False(customGame.IsLegalMove(illegalCapture), "Legal move generation should never allow directly capturing the opposing king.");
    Assert.NotContains(illegalCapture, customGame.GetLegalMoves(ChessPosition.Parse("e7")), "The opposing king square must be excluded from legal move generation.");

    var checkedGame = CreateGame(
        ChessColor.Black,
        CastlingRights.None,
        ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("e7", new ChessPiece(ChessColor.White, PieceType.Queen)),
        ("e8", new ChessPiece(ChessColor.Black, PieceType.King)));

    Assert.Equal(ChessGameStatus.Check, checkedGame.GetStatus(), "Custom positions should resolve via check semantics instead of king-capture moves.");
}

static void CastlingIsGeneratedWhenLegal()
{
    var game = CreateGame(
        ChessColor.White,
        new CastlingRights(true, true, false, false),
        ("e1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("a1", new ChessPiece(ChessColor.White, PieceType.Rook)),
        ("h1", new ChessPiece(ChessColor.White, PieceType.Rook)),
        ("e8", new ChessPiece(ChessColor.Black, PieceType.King)));

    var legalMoves = game.GetLegalMoves();
    Assert.Contains(ChessMove.Parse("e1g1"), legalMoves, "Kingside castling should be legal.");
    Assert.Contains(ChessMove.Parse("e1c1"), legalMoves, "Queenside castling should be legal.");
}

static void CastlingThroughCheckIsRejected()
{
    var game = CreateGame(
        ChessColor.White,
        new CastlingRights(true, false, false, false),
        ("e1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("h1", new ChessPiece(ChessColor.White, PieceType.Rook)),
        ("a8", new ChessPiece(ChessColor.Black, PieceType.King)),
        ("f8", new ChessPiece(ChessColor.Black, PieceType.Rook)));

    Assert.False(game.IsLegalMove(ChessMove.Parse("e1g1")), "Kingside castling should fail when f1 is attacked.");
}

static void EnPassantIsAvailableImmediately()
{
    var game = ChessGame.CreateInitial()
        .ApplyMove(ChessMove.Parse("e2e4"))
        .ApplyMove(ChessMove.Parse("a7a6"))
        .ApplyMove(ChessMove.Parse("e4e5"))
        .ApplyMove(ChessMove.Parse("d7d5"));

    var enPassant = ChessMove.Parse("e5d6");
    Assert.True(game.IsLegalMove(enPassant), "En passant should be legal immediately after the double-step pawn move.");

    var resolved = game.ApplyMove(enPassant);
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Pawn), resolved.Board[ChessPosition.Parse("d6")], "The white pawn should land on d6.");
    Assert.Null(resolved.Board[ChessPosition.Parse("d5")], "The black pawn should be removed from d5.");
}

static void EnPassantExpiresAfterAnInterveningTurn()
{
    var game = ChessGame.CreateInitial()
        .ApplyMove(ChessMove.Parse("e2e4"))
        .ApplyMove(ChessMove.Parse("a7a6"))
        .ApplyMove(ChessMove.Parse("e4e5"))
        .ApplyMove(ChessMove.Parse("d7d5"))
        .ApplyMove(ChessMove.Parse("b1c3"))
        .ApplyMove(ChessMove.Parse("a6a5"));

    Assert.False(game.IsLegalMove(ChessMove.Parse("e5d6")), "En passant should no longer be legal after a full intervening turn.");
}

static void PromotionGeneratesAllStandardChoices()
{
    var game = CreateGame(
        ChessColor.White,
        CastlingRights.None,
        ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("g7", new ChessPiece(ChessColor.White, PieceType.Pawn)),
        ("a8", new ChessPiece(ChessColor.Black, PieceType.King)));

    var legalMoves = game.GetLegalMoves(ChessPosition.Parse("g7"));
    Assert.Contains(ChessMove.Parse("g7g8q"), legalMoves, "Queen promotion should be legal.");
    Assert.Contains(ChessMove.Parse("g7g8r"), legalMoves, "Rook promotion should be legal.");
    Assert.Contains(ChessMove.Parse("g7g8b"), legalMoves, "Bishop promotion should be legal.");
    Assert.Contains(ChessMove.Parse("g7g8n"), legalMoves, "Knight promotion should be legal.");

    var promoted = game.ApplyMove(ChessMove.Parse("g7g8q"));
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Queen), promoted.Board[ChessPosition.Parse("g8")], "Promotion should replace the pawn with the chosen piece.");
}

static void KnownPositionIsStalemate()
{
    var game = CreateGame(
        ChessColor.Black,
        CastlingRights.None,
        ("c6", new ChessPiece(ChessColor.White, PieceType.King)),
        ("c7", new ChessPiece(ChessColor.White, PieceType.Queen)),
        ("a8", new ChessPiece(ChessColor.Black, PieceType.King)));

    Assert.Equal(ChessGameStatus.Stalemate, game.GetStatus(), "This classic king-and-queen layout should be stalemate.");
    Assert.Equal(0, game.GetLegalMoves().Count, "Black should have no legal moves in stalemate.");
    Assert.False(game.IsInCheck(ChessColor.Black), "The stalemated king must not be in check.");
}

static ChessGame CreateGame(
    ChessColor sideToMove,
    CastlingRights castlingRights,
    params (string Square, ChessPiece Piece)[] pieces)
{
    var board = ChessBoard.CreateEmpty();

    foreach (var (square, piece) in pieces)
    {
        board = board.WithPiece(ChessPosition.Parse(square), piece);
    }

    return new ChessGame(board, sideToMove, castlingRights);
}

static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool condition, string message)
    {
        True(!condition, message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
        }
    }

    public static void Contains<T>(T expected, IEnumerable<T> values, string message)
    {
        if (!values.Contains(expected))
        {
            throw new InvalidOperationException($"{message} Missing: {expected}.");
        }
    }

    public static void NotContains<T>(T expected, IEnumerable<T> values, string message)
    {
        if (values.Contains(expected))
        {
            throw new InvalidOperationException($"{message} Unexpected: {expected}.");
        }
    }

    public static void Null<T>(T? value, string message)
        where T : struct
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"{message} Actual: {value}.");
        }
    }
}