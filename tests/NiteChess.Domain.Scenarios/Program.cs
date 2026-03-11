using NiteChess.Application.ComputerPlay;
using NiteChess.Application.GameSessions;
using NiteChess.Domain.Chess;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

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
    ("Known king and queen layout is stalemate", KnownPositionIsStalemate),
    ("Session history tracks applied local moves", SessionHistoryTracksAppliedLocalMoves),
    ("Promotion selection pauses play until a choice is made", PromotionSelectionPausesPlayUntilChoice),
    ("Persistence restores completed move history and supports resume", PersistenceRestoresMoveHistoryAndSupportsResume),
    ("Persistence restores pending promotion state", PersistenceRestoresPendingPromotionState),
    ("Stockfish FEN serialization matches the initial position", StockfishFenSerializerMatchesInitialPosition),
    ("AI difficulty presets map to deterministic Stockfish profiles", AiDifficultyPresetsMapToDeterministicStockfishProfiles),
    ("Stockfish command builder emits deterministic UCI commands", StockfishCommandBuilderEmitsDeterministicUciCommands),
    ("Native-process Stockfish path can smoke-request one engine move", NativeProcessStockfishPathCanSmokeRequestOneEngineMove),
    ("Native-library Stockfish path can parse one bridged engine move", NativeLibraryStockfishPathCanParseOneBridgedEngineMove),
    ("Computer move service requests and validates an engine move", ComputerMoveServiceRequestsAndValidatesEngineMove),
    ("Computer move service rejects sessions with pending promotion", ComputerMoveServiceRejectsPendingPromotionSessions)
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

static void SessionHistoryTracksAppliedLocalMoves()
{
    var service = new GameSessionService();
    var session = service.CreateSession();

    var firstMove = service.SubmitMove(session, ChessMove.Parse("e2e4"));
    Assert.Equal(GameSessionMoveOutcome.Applied, firstMove.Outcome, "Opening move should apply successfully.");
    Assert.NotNull(firstMove.AppliedMove, "Applied moves should surface a history record.");

    session = firstMove.Session;
    Assert.Equal(1, session.MoveHistory.Count, "Applied moves should be recorded in session history.");
    Assert.Equal("1. e2e4", session.MoveHistory[0].DisplayText, "White moves should render with turn-number display text.");
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Pawn), session.MoveHistory[0].Piece, "History should capture the moving piece.");
    Assert.False(session.MoveHistory[0].IsCapture, "e2e4 should not be recorded as a capture.");

    var secondMove = service.SubmitMove(session, ChessMove.Parse("e7e5"));
    Assert.Equal(GameSessionMoveOutcome.Applied, secondMove.Outcome, "Reply move should apply successfully.");

    session = secondMove.Session;
    Assert.Equal(2, session.MoveHistory.Count, "Two plies should produce two history records.");
    Assert.Equal("1... e7e5", session.MoveHistory[1].DisplayText, "Black moves should render with ellipsis display text.");
    Assert.Equal(ChessColor.White, session.Game.SideToMove, "After two plies it should be White to move again.");
}

static void PromotionSelectionPausesPlayUntilChoice()
{
    var service = new GameSessionService();
    var session = service.CreateSession(CreateGame(
        ChessColor.White,
        CastlingRights.None,
        ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
        ("g7", new ChessPiece(ChessColor.White, PieceType.Pawn)),
        ("a8", new ChessPiece(ChessColor.Black, PieceType.King))));

    var pendingMove = service.SubmitMove(session, ChessPosition.Parse("g7"), ChessPosition.Parse("g8"));
    Assert.Equal(GameSessionMoveOutcome.PromotionSelectionRequired, pendingMove.Outcome, "Promotion should pause for a piece choice.");
    Assert.NotNull(pendingMove.Session.PendingPromotion, "Pending promotion state should be retained on the session.");
    Assert.Equal(0, pendingMove.Session.MoveHistory.Count, "History should not advance before the promotion choice is made.");
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Pawn), pendingMove.Session.Game.Board[ChessPosition.Parse("g7")], "The pawn should remain on its source square while promotion is pending.");
    Assert.SequenceEqual(
        new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight },
        pendingMove.Session.PendingPromotion!.AvailablePieceTypes,
        "Promotion options should expose the four standard pieces in domain order.");

    var promoted = service.CompletePromotion(pendingMove.Session, PieceType.Knight);
    Assert.Equal(GameSessionMoveOutcome.Applied, promoted.Outcome, "Selecting a promotion piece should apply the move.");
    Assert.Null(promoted.Session.PendingPromotion, "Pending promotion state should clear after completion.");
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Knight), promoted.Session.Game.Board[ChessPosition.Parse("g8")], "Promotion should place the chosen piece on the target square.");
    Assert.Equal(PieceType.Knight, promoted.Session.MoveHistory[0].PromotionPieceType, "History should preserve the chosen promotion piece.");
}

static void PersistenceRestoresMoveHistoryAndSupportsResume()
{
    var service = new GameSessionService();
    var persistence = new GameSessionPersistenceService();
    var session = service.CreateSession();

    session = service.SubmitMove(session, ChessMove.Parse("e2e4")).Session;
    session = service.SubmitMove(session, ChessMove.Parse("e7e5")).Session;
    session = service.SubmitMove(session, ChessMove.Parse("g1f3")).Session;

    var payload = persistence.Save(session);
    var restored = persistence.Load(payload);

    Assert.Equal(session.SessionId, restored.SessionId, "Persistence should keep the same session identifier.");
    Assert.Equal(session.MoveHistory.Count, restored.MoveHistory.Count, "Persistence should restore the move history length.");
    Assert.Equal(session.Game.SideToMove, restored.Game.SideToMove, "Persistence should restore the active player.");
    Assert.Equal(session.Game.CastlingRights, restored.Game.CastlingRights, "Persistence should restore castling rights.");
    Assert.Equal(session.Game.EnPassantTarget, restored.Game.EnPassantTarget, "Persistence should restore the en passant target.");
    Assert.Equal(session.Game.HalfmoveClock, restored.Game.HalfmoveClock, "Persistence should restore the halfmove clock.");
    Assert.Equal(session.Game.FullmoveNumber, restored.Game.FullmoveNumber, "Persistence should restore the fullmove number.");
    Assert.Equal(session.MoveHistory[2].DisplayText, restored.MoveHistory[2].DisplayText, "Persistence should restore UI-friendly history text.");
    AssertBoardEqual(session.Game.Board, restored.Game.Board, "Persistence should restore every occupied square.");

    var resumed = service.SubmitMove(restored, ChessMove.Parse("b8c6"));
    Assert.Equal(GameSessionMoveOutcome.Applied, resumed.Outcome, "A restored session should be immediately resumable.");
    Assert.Equal(4, resumed.Session.MoveHistory.Count, "Resumed play should continue the existing history.");
}

static void PersistenceRestoresPendingPromotionState()
{
    var service = new GameSessionService();
    var persistence = new GameSessionPersistenceService();
    var pendingSession = service.SubmitMove(
            service.CreateSession(CreateGame(
                ChessColor.White,
                CastlingRights.None,
                ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
                ("g7", new ChessPiece(ChessColor.White, PieceType.Pawn)),
                ("a8", new ChessPiece(ChessColor.Black, PieceType.King)))),
            ChessPosition.Parse("g7"),
            ChessPosition.Parse("g8"))
        .Session;

    var payload = persistence.Save(pendingSession);
    var restored = persistence.Load(payload);

    Assert.NotNull(restored.PendingPromotion, "Pending promotion state should survive save/load round-trips.");
    Assert.Equal("g7g8", restored.PendingPromotion!.CoordinateNotation, "Pending promotion coordinates should be restored.");
    Assert.SequenceEqual(
        pendingSession.PendingPromotion!.AvailablePieceTypes,
        restored.PendingPromotion.AvailablePieceTypes,
        "Pending promotion choices should be preserved through persistence.");

    var promoted = service.CompletePromotion(restored, PieceType.Queen);
    Assert.Equal(GameSessionMoveOutcome.Applied, promoted.Outcome, "Restored pending promotion should still be completable.");
    Assert.Equal(new ChessPiece(ChessColor.White, PieceType.Queen), promoted.Session.Game.Board[ChessPosition.Parse("g8")], "Restored pending promotion should resume into the chosen piece.");
}

static void StockfishFenSerializerMatchesInitialPosition()
{
    Assert.Equal(
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        StockfishFenSerializer.ToFen(ChessGame.CreateInitial()),
        "Initial chess position should serialize to the canonical starting FEN.");
}

static void AiDifficultyPresetsMapToDeterministicStockfishProfiles()
{
    foreach (var (difficulty, presetId, depth) in new[]
             {
                 (AiDifficulty.Easy, "easy", 4),
                 (AiDifficulty.Medium, "medium", 8),
                 (AiDifficulty.Hard, "hard", 12),
                 (AiDifficulty.Expert, "expert", 16)
             })
    {
        var engine = new CapturingStockfishEngineClient("e2e4");
        var service = new StockfishComputerMoveService(engine);

        _ = GetComputerMove(service, new GameSessionService().CreateSession(), difficulty);

        Assert.NotNull(engine.LastRequest, $"Difficulty '{difficulty}' should issue a Stockfish request.");
        Assert.Equal(presetId, engine.LastRequest!.SearchConfiguration.PresetId, $"Difficulty '{difficulty}' should map to the expected preset id.");
        Assert.Equal(depth, engine.LastRequest.SearchConfiguration.SearchDepth, $"Difficulty '{difficulty}' should map to a deterministic search depth.");
        Assert.Equal(1, engine.LastRequest.SearchConfiguration.Threads, $"Difficulty '{difficulty}' should pin Stockfish to a single thread for deterministic offline play.");
        Assert.Equal(16, engine.LastRequest.SearchConfiguration.HashMegabytes, $"Difficulty '{difficulty}' should keep a stable hash size.");
        Assert.False(engine.LastRequest.SearchConfiguration.PonderEnabled, $"Difficulty '{difficulty}' should keep ponder disabled.");
    }
}

static void StockfishCommandBuilderEmitsDeterministicUciCommands()
{
    var commands = StockfishUciCommandBuilder.Build(
        new StockfishEngineRequest(
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            new StockfishSearchConfiguration("expert", 16, 1, 16, 1, 0, false)));

    Assert.SequenceEqual(
        new[]
        {
            "uci",
            "setoption name Threads value 1",
            "setoption name Hash value 16",
            "setoption name MultiPV value 1",
            "setoption name Move Overhead value 0",
            "setoption name Ponder value false",
            "setoption name UCI_Chess960 value false",
            "setoption name UCI_LimitStrength value false",
            "setoption name UCI_ShowWDL value false",
            "ucinewgame",
            "isready",
            "position fen rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "go depth 16"
        },
        commands,
        "Stockfish command builder should emit the stable UCI sequence used by the offline AI abstraction.");
}

static void NativeProcessStockfishPathCanSmokeRequestOneEngineMove()
{
    var runtimePath = CreateFakeNativeStockfishRuntime();

    try
    {
        var engine = new RuntimeConfiguredStockfishEngineClient(
            new StockfishRuntimeDescriptor(
                HostId: "scenario",
                IntegrationMode: StockfishIntegrationMode.NativeProcess,
                RuntimeLocation: runtimePath,
                IsBundled: true,
                Notes: "Scenario-local fake UCI engine."));
        var service = new StockfishComputerMoveService(engine);

        var move = GetComputerMove(service, new GameSessionService().CreateSession(), AiDifficulty.Hard);

        Assert.Equal(
            ChessMove.Parse("e2e4"),
            move,
            "The native-process Stockfish path should support a smoke move request through the shared abstraction.");
    }
    finally
    {
        DeleteParentDirectory(runtimePath);
    }
}

static void NativeLibraryStockfishPathCanParseOneBridgedEngineMove()
{
    var engine = new RuntimeConfiguredStockfishEngineClient(
        new StockfishRuntimeDescriptor(
            HostId: "mobile-test",
            IntegrationMode: StockfishIntegrationMode.NativeLibrary,
            RuntimeLocation: "Resources/Raw/Stockfish/native/android-arm64-v8a/libnitechess_stockfish_bridge",
            IsBundled: true,
            Notes: "Scenario-local mobile bridge stub."),
        new[]
        {
            new TestNativeLibraryStockfishEngineClientFactory(_ => "bestmove e2e4 ponder e7e5")
        });
    var service = new StockfishComputerMoveService(engine);

    var move = GetComputerMove(service, new GameSessionService().CreateSession(), AiDifficulty.Medium);

    Assert.Equal(
        ChessMove.Parse("e2e4"),
        move,
        "The native-library Stockfish path should support a bridged engine bestmove response through the shared abstraction.");
}

static void ComputerMoveServiceRequestsAndValidatesEngineMove()
{
    var engine = new CapturingStockfishEngineClient("e2e4");
    var service = new StockfishComputerMoveService(engine);
    var move = GetComputerMove(service, new GameSessionService().CreateSession(), AiDifficulty.Medium);

    Assert.Equal(ChessMove.Parse("e2e4"), move, "The computer move service should parse the engine best move response.");
    Assert.NotNull(engine.LastRequest, "A Stockfish request should be captured for move generation.");
    Assert.Equal(
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        engine.LastRequest!.PositionFen,
        "The computer move service should send the current position as FEN.");
    Assert.SequenceEqual(
        StockfishUciCommandBuilder.Build(engine.LastRequest),
        engine.LastResponse!.Commands,
        "The captured engine response should retain the deterministic command list for inspection.");
}

static void ComputerMoveServiceRejectsPendingPromotionSessions()
{
    var sessionService = new GameSessionService();
    var pendingPromotion = sessionService.SubmitMove(
            sessionService.CreateSession(CreateGame(
                ChessColor.White,
                CastlingRights.None,
                ("h1", new ChessPiece(ChessColor.White, PieceType.King)),
                ("g7", new ChessPiece(ChessColor.White, PieceType.Pawn)),
                ("a8", new ChessPiece(ChessColor.Black, PieceType.King)))),
            ChessPosition.Parse("g7"),
            ChessPosition.Parse("g8"))
        .Session;
    var service = new StockfishComputerMoveService(new CapturingStockfishEngineClient("a8a7"));

    Assert.Throws<InvalidOperationException>(
        () => GetComputerMove(service, pendingPromotion, AiDifficulty.Easy),
        "Computer move requests should be rejected while a promotion choice is pending.");
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

static ChessMove GetComputerMove(IComputerMoveService service, LocalGameSession session, AiDifficulty difficulty)
{
    return service.GetMoveAsync(session, difficulty).AsTask().GetAwaiter().GetResult();
}

static string CreateFakeNativeStockfishRuntime()
{
    var runtimeDirectory = Path.Combine(Path.GetTempPath(), $"nitechess-stockfish-{Guid.NewGuid():N}");
    Directory.CreateDirectory(runtimeDirectory);

    if (OperatingSystem.IsWindows())
    {
        var runtimePath = Path.Combine(runtimeDirectory, "fake-stockfish.cmd");
        File.WriteAllText(
            runtimePath,
            "@echo off\r\n" +
            "setlocal EnableExtensions EnableDelayedExpansion\r\n" +
            ":loop\r\n" +
            "set line=\r\n" +
            "set /p line=\r\n" +
            "if errorlevel 1 goto :eof\r\n" +
            "if \"!line!\"==\"uci\" (echo id name FakeStockfish& echo uciok)\r\n" +
            "if \"!line!\"==\"isready\" echo readyok\r\n" +
            "echo !line!| findstr /b /c:\"go \" >nul && echo bestmove e2e4 ponder e7e5\r\n" +
            "if \"!line!\"==\"quit\" goto :eof\r\n" +
            "goto loop\r\n");
        return runtimePath;
    }

    var unixRuntimePath = Path.Combine(runtimeDirectory, "fake-stockfish");
    File.WriteAllText(
        unixRuntimePath,
        "#!/usr/bin/env python3\n" +
        "import sys\n" +
        "for raw in sys.stdin:\n" +
        "    line = raw.strip()\n" +
        "    if line == 'uci':\n" +
        "        print('id name FakeStockfish')\n" +
        "        print('uciok')\n" +
        "    elif line == 'isready':\n" +
        "        print('readyok')\n" +
        "    elif line.startswith('go '):\n" +
        "        print('bestmove e2e4 ponder e7e5')\n" +
        "    elif line == 'quit':\n" +
        "        break\n" +
        "    sys.stdout.flush()\n");
    File.SetUnixFileMode(
        unixRuntimePath,
        UnixFileMode.UserRead |
        UnixFileMode.UserWrite |
        UnixFileMode.UserExecute |
        UnixFileMode.GroupRead |
        UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead |
        UnixFileMode.OtherExecute);
    return unixRuntimePath;
}

static void DeleteParentDirectory(string filePath)
{
    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
    {
        Directory.Delete(directory, recursive: true);
    }
}

sealed class CapturingStockfishEngineClient : IStockfishEngineClient
{
    private readonly string _bestMoveNotation;

    public CapturingStockfishEngineClient(string bestMoveNotation)
    {
        _bestMoveNotation = bestMoveNotation;
    }

    public StockfishEngineRequest? LastRequest { get; private set; }

    public StockfishEngineResponse? LastResponse { get; private set; }

    public ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        LastResponse = new StockfishEngineResponse(
            _bestMoveNotation,
            null,
            StockfishUciCommandBuilder.Build(request));
        return ValueTask.FromResult(LastResponse);
    }
}

sealed class TestNativeLibraryStockfishEngineClientFactory : IStockfishEngineClientFactory
{
    private readonly Func<string, string> _bridgeInvoker;

    public TestNativeLibraryStockfishEngineClientFactory(Func<string, string> bridgeInvoker)
    {
        _bridgeInvoker = bridgeInvoker;
    }

    public bool CanCreate(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        return runtimeDescriptor.IntegrationMode == StockfishIntegrationMode.NativeLibrary;
    }

    public IStockfishEngineClient Create(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        return new TestNativeLibraryStockfishEngineClient(runtimeDescriptor, _bridgeInvoker);
    }
}

sealed class TestNativeLibraryStockfishEngineClient : IStockfishEngineClient
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;
    private readonly Func<string, string> _bridgeInvoker;

    public TestNativeLibraryStockfishEngineClient(
        StockfishRuntimeDescriptor runtimeDescriptor,
        Func<string, string> bridgeInvoker)
    {
        _runtimeDescriptor = runtimeDescriptor;
        _bridgeInvoker = bridgeInvoker;
    }

    public ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        var commands = StockfishUciCommandBuilder.Build(request).ToArray();
        var line = _bridgeInvoker(string.Join('\n', commands));
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2 || parts[0] != "bestmove")
        {
            throw new InvalidOperationException($"Malformed bridged native-library bestmove output '{line}' for '{_runtimeDescriptor.RuntimeLocation}'.");
        }

        var ponder = parts.Length >= 4 && parts[2] == "ponder" ? parts[3] : null;
        return ValueTask.FromResult(new StockfishEngineResponse(parts[1], ponder, commands.Concat(new[] { line }).ToArray()));
    }
}

static void AssertBoardEqual(ChessBoard expected, ChessBoard actual, string message)
{
    for (var file = 0; file < 8; file++)
    {
        for (var rank = 0; rank < 8; rank++)
        {
            var position = new ChessPosition(file, rank);
            Assert.Equal(expected[position], actual[position], $"{message} Square {position} differed.");
        }
    }
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

    public static void Null<T>(T? value, string message)
        where T : class
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"{message} Actual: {value}.");
        }
    }

    public static void NotNull<T>(T? value, string message)
        where T : class
    {
        if (value is null)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
    {
        var expectedValues = expected.ToArray();
        var actualValues = actual.ToArray();

        if (!expectedValues.SequenceEqual(actualValues))
        {
            throw new InvalidOperationException(
                $"{message} Expected: [{string.Join(", ", expectedValues)}]. Actual: [{string.Join(", ", actualValues)}].");
        }
    }

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{message} Expected exception: {typeof(TException).Name}. Actual: {exception.GetType().Name}.");
        }

        throw new InvalidOperationException($"{message} Expected exception: {typeof(TException).Name}. Actual: none.");
    }
}