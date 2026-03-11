using NiteChess.Application.ComputerPlay;
using NiteChess.Application.GameSessions;
using NiteChess.Domain.Chess;
using NiteChess.Online;
using NiteChess.Online.Contracts;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Application.Gameplay;

public sealed class GameplayController
{
    private static readonly IReadOnlyList<AiDifficulty> DifficultyOptions = Enum.GetValues<AiDifficulty>();
    private static readonly IReadOnlyList<ChessColor> HumanColorOptions = new[] { ChessColor.White, ChessColor.Black };

    private readonly IGameSessionService _sessionService;
    private readonly IGameSessionPersistenceService _persistenceService;
    private readonly IComputerMoveService _computerMoveService;
    private readonly IStockfishRuntimeBootstrapper _runtimeBootstrapper;
    private readonly IOnlineGameClient _onlineGameClient;
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;

    private LocalGameSession _session;
    private ChessPosition? _selectedSquare;
    private ChessColor _boardPerspective;
    private ChessColor? _computerPlayerColor;
    private AiDifficulty _selectedDifficulty;
    private string _messageText;
    private string _saveDraft;
    private string _onlineServerUrl;
    private string _onlinePlayerName;
    private string _onlineRoomCode;
    private string _onlinePlayerToken;
    private string _onlineConnectionSummary;
    private bool _isBusy;
    private bool _runtimeInitialized;
    private bool _isOnlineGame;
    private bool _onlineIsConnected;
    private bool _onlineIsReconnecting;
    private ChessColor? _onlinePlayerColor;
    private OnlineGameRoomState? _onlineRoomState;

    public GameplayController(
        IGameSessionService sessionService,
        IGameSessionPersistenceService persistenceService,
        IComputerMoveService computerMoveService,
        IStockfishRuntimeBootstrapper runtimeBootstrapper,
        IOnlineGameClient onlineGameClient)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _computerMoveService = computerMoveService ?? throw new ArgumentNullException(nameof(computerMoveService));
        _runtimeBootstrapper = runtimeBootstrapper ?? throw new ArgumentNullException(nameof(runtimeBootstrapper));
        _onlineGameClient = onlineGameClient ?? throw new ArgumentNullException(nameof(onlineGameClient));
        _runtimeDescriptor = runtimeBootstrapper.Describe();
        _session = _sessionService.CreateSession();
        _boardPerspective = ChessColor.White;
        _selectedDifficulty = AiDifficulty.Medium;
        _messageText = "New two-player game ready.";
        _saveDraft = string.Empty;
        _onlineServerUrl = string.Empty;
        _onlinePlayerName = string.Empty;
        _onlineRoomCode = string.Empty;
        _onlinePlayerToken = string.Empty;
        _onlineConnectionSummary = "Not connected.";
        _onlineGameClient.RoomStateChanged += OnOnlineRoomStateChanged;
        _onlineGameClient.ConnectionStatusChanged += OnOnlineConnectionStatusChanged;
        State = BuildState();
    }

    public event EventHandler? StateChanged;

    public GameplayViewState State { get; private set; }

    public IReadOnlyList<AiDifficulty> AvailableDifficulties => DifficultyOptions;

    public IReadOnlyList<ChessColor> AvailableHumanColors => HumanColorOptions;

    public async ValueTask StartLocalGameAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ExitOnlineModeAsync(cancellationToken);
        _session = _sessionService.CreateSession();
        _selectedSquare = null;
        _computerPlayerColor = null;
        _boardPerspective = ChessColor.White;
        _saveDraft = string.Empty;
        _messageText = "New two-player game ready.";
        PublishState();
    }

    public async ValueTask StartComputerGameAsync(
        ChessColor humanColor,
        AiDifficulty difficulty,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ExitOnlineModeAsync(cancellationToken);
        _session = _sessionService.CreateSession();
        _selectedSquare = null;
        _computerPlayerColor = GetOpponent(humanColor);
        _boardPerspective = humanColor;
        _selectedDifficulty = difficulty;
        _saveDraft = string.Empty;
        _messageText = $"New game versus {_selectedDifficulty} computer. You play {FormatColor(humanColor)}.";
        PublishState();
        await RunComputerTurnIfNeededAsync(cancellationToken);
    }

    public void UpdateOnlineServerUrl(string? serverUrl)
    {
        _onlineServerUrl = serverUrl?.Trim() ?? string.Empty;
        PublishState();
    }

    public void UpdateOnlinePlayerName(string? playerName)
    {
        _onlinePlayerName = playerName?.Trim() ?? string.Empty;
        PublishState();
    }

    public void UpdateOnlineRoomCode(string? roomCode)
    {
        _onlineRoomCode = string.IsNullOrWhiteSpace(roomCode)
            ? string.Empty
            : roomCode.Trim().ToUpperInvariant();
        PublishState();
    }

    public void UpdateOnlinePlayerToken(string? playerToken)
    {
        _onlinePlayerToken = playerToken?.Trim() ?? string.Empty;
        PublishState();
    }

    public async ValueTask CreateOnlineGameAsync(CancellationToken cancellationToken = default)
    {
        await StartOnlineOperationAsync(
            "Creating online room…",
            requirePlayerName: true,
            async serverUri =>
            {
                var result = await _onlineGameClient.CreateRoomAsync(serverUri, _onlinePlayerName, cancellationToken);
                ApplyOnlineConnectionResult(result);
            },
            cancellationToken);
    }

    public async ValueTask JoinOnlineGameAsync(CancellationToken cancellationToken = default)
    {
        await StartOnlineOperationAsync(
            $"Joining room {_onlineRoomCode}…",
            requirePlayerName: true,
            async serverUri =>
            {
                var result = await _onlineGameClient.JoinRoomAsync(serverUri, _onlineRoomCode, _onlinePlayerName, cancellationToken);
                ApplyOnlineConnectionResult(result);
            },
            cancellationToken);
    }

    public async ValueTask ResumeOnlineGameAsync(CancellationToken cancellationToken = default)
    {
        await StartOnlineOperationAsync(
            $"Resuming room {_onlineRoomCode}…",
            requirePlayerName: false,
            async serverUri =>
            {
                var result = await _onlineGameClient.ResumeRoomAsync(serverUri, _onlineRoomCode, _onlinePlayerToken, cancellationToken);
                ApplyOnlineConnectionResult(result);
            },
            cancellationToken);
    }

    public async ValueTask SelectSquareAsync(ChessPosition position, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_isBusy)
        {
            _messageText = _isOnlineGame
                ? "Waiting for the server to finish the last online action."
                : "The computer is thinking. Please wait for its move.";
            PublishState();
            return;
        }

        if (_session.PendingPromotion is not null)
        {
            _messageText = $"Choose a promotion piece for {_session.PendingPromotion.CoordinateNotation} before continuing.";
            PublishState();
            return;
        }

        if (IsWaitingForOnlineOpponent())
        {
            _messageText = "Waiting for a second player to join the online room.";
            PublishState();
            return;
        }

        if (IsComputerTurn())
        {
            _messageText = "It is the computer's turn.";
            PublishState();
            return;
        }

        if (IsRemoteOpponentTurn())
        {
            _messageText = "It is the remote opponent's turn.";
            PublishState();
            return;
        }

        if (_selectedSquare is null)
        {
            SelectOwnPiece(position);
            return;
        }

        if (_selectedSquare.Value == position)
        {
            _selectedSquare = null;
            _messageText = "Selection cleared.";
            PublishState();
            return;
        }

        var targetPiece = _session.Game.Board[position];
        if (targetPiece is ChessPiece ownPiece && ownPiece.Color == _session.Game.SideToMove)
        {
            SelectOwnPiece(position);
            return;
        }

        if (_isOnlineGame)
        {
            await SubmitOnlineMoveAsync(_selectedSquare.Value, position, cancellationToken);
            return;
        }

        var result = _sessionService.SubmitMove(_session, _selectedSquare.Value, position);
        ApplyMoveResult(result, result.AppliedMove is null ? null : $"Played {result.AppliedMove.DisplayText}.");

        if (result.Outcome is GameSessionMoveOutcome.Applied or GameSessionMoveOutcome.PromotionSelectionRequired)
        {
            await RunComputerTurnIfNeededAsync(cancellationToken);
        }
    }

    public async ValueTask ChoosePromotionAsync(PieceType promotionPieceType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_session.PendingPromotion is null)
        {
            _messageText = "No promotion is awaiting a selection.";
            PublishState();
            return;
        }

        if (_isOnlineGame)
        {
            if (!CanChoosePromotion())
            {
                _messageText = "Waiting for the online player responsible for the promotion choice.";
                PublishState();
                return;
            }

            await SubmitOnlinePromotionAsync(promotionPieceType, cancellationToken);
            return;
        }

        var result = _sessionService.CompletePromotion(_session, promotionPieceType);
        ApplyMoveResult(result, $"Promotion completed as {FormatPieceType(promotionPieceType)}.");

        if (result.Outcome == GameSessionMoveOutcome.Applied)
        {
            await RunComputerTurnIfNeededAsync(cancellationToken);
        }
    }

    public void SaveSnapshot()
    {
        _saveDraft = _persistenceService.Save(_session);
        _messageText = "Session serialized into the snapshot box.";
        PublishState();
    }

    public async ValueTask LoadSnapshotAsync(string? snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_isOnlineGame)
        {
            _messageText = "Manual snapshot load is disabled during online games because the server remains authoritative.";
            PublishState();
            return;
        }

        if (string.IsNullOrWhiteSpace(snapshot))
        {
            _messageText = "Paste a saved session payload before loading.";
            PublishState();
            return;
        }

        try
        {
            _session = _persistenceService.Load(snapshot);
            _selectedSquare = null;
            _saveDraft = snapshot;
            _messageText = "Saved session restored.";
            PublishState();
            await RunComputerTurnIfNeededAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            _messageText = $"Load failed: {exception.Message}";
            PublishState();
        }
    }

    public void UpdateSaveDraft(string? snapshot)
    {
        var nextDraft = snapshot ?? string.Empty;
        if (string.Equals(_saveDraft, nextDraft, StringComparison.Ordinal))
        {
            return;
        }

        _saveDraft = nextDraft;
        PublishState();
    }

    private async ValueTask StartOnlineOperationAsync(
        string progressMessage,
        bool requirePlayerName,
        Func<Uri, Task> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _isBusy = true;
        _selectedSquare = null;
        _messageText = progressMessage;
        PublishState();

        try
        {
            var serverUri = ParseOnlineServerUri(requirePlayerName);
            await operation(serverUri);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UriFormatException)
        {
            _messageText = $"Online connection failed: {exception.Message}";
            PublishState();
        }
        finally
        {
            _isBusy = false;
            PublishState();
        }
    }

    private async ValueTask ExitOnlineModeAsync(CancellationToken cancellationToken)
    {
        if (!_isOnlineGame && !_onlineIsConnected && !_onlineIsReconnecting)
        {
            return;
        }

        try
        {
            await _onlineGameClient.DisconnectAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _onlineConnectionSummary = $"Online disconnect warning: {exception.Message}";
        }

        _isOnlineGame = false;
        _onlineIsConnected = false;
        _onlineIsReconnecting = false;
        _onlinePlayerColor = null;
        _onlineRoomState = null;
        _onlineConnectionSummary = "Not connected.";
    }

    private void ApplyOnlineConnectionResult(OnlineRoomConnectionResult result)
    {
        _isOnlineGame = true;
        _onlineIsConnected = true;
        _onlineIsReconnecting = false;
        _onlinePlayerColor = result.PlayerColor;
        _onlinePlayerName = result.PlayerName;
        _onlineRoomCode = result.RoomCode;
        _onlinePlayerToken = result.PlayerToken;
        _boardPerspective = result.PlayerColor;
        ApplyOnlineRoomState(result.RoomState);
    }

    private void ApplyOnlineRoomState(OnlineGameRoomState roomState)
    {
        _onlineRoomState = roomState;
        _onlineRoomCode = roomState.RoomCode;
        _isOnlineGame = true;
        _selectedSquare = null;
        _session = _persistenceService.Load(roomState.SessionSnapshot);
        _saveDraft = roomState.SessionSnapshot;

        if (_onlinePlayerColor is ChessColor playerColor)
        {
            _boardPerspective = playerColor;
        }

        _messageText = roomState.StatusMessage;
        PublishState();
    }

    private async ValueTask SubmitOnlineMoveAsync(ChessPosition from, ChessPosition to, CancellationToken cancellationToken)
    {
        _isBusy = true;
        _messageText = $"Submitting {from}{to} to the server…";
        PublishState();

        try
        {
            var result = await _onlineGameClient.SubmitMoveAsync(from.ToString(), to.ToString(), cancellationToken);
            _selectedSquare = result.Accepted ? null : _selectedSquare;
            _messageText = result.Message;
            if (result.Accepted)
            {
                ApplyOnlineRoomState(result.RoomState);
            }
            else
            {
                PublishState();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _messageText = $"Online move failed: {exception.Message}";
            PublishState();
        }
        finally
        {
            _isBusy = false;
            PublishState();
        }
    }

    private async ValueTask SubmitOnlinePromotionAsync(PieceType promotionPieceType, CancellationToken cancellationToken)
    {
        _isBusy = true;
        _messageText = $"Submitting promotion choice {FormatPieceType(promotionPieceType)}…";
        PublishState();

        try
        {
            var result = await _onlineGameClient.CompletePromotionAsync(promotionPieceType, cancellationToken);
            _messageText = result.Message;
            if (result.Accepted)
            {
                ApplyOnlineRoomState(result.RoomState);
            }
            else
            {
                PublishState();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _messageText = $"Promotion submission failed: {exception.Message}";
            PublishState();
        }
        finally
        {
            _isBusy = false;
            PublishState();
        }
    }

    private void SelectOwnPiece(ChessPosition position)
    {
        var piece = _session.Game.Board[position];

        if (piece is not ChessPiece boardPiece || boardPiece.Color != _session.Game.SideToMove)
        {
            _messageText = $"Select a {FormatColor(_session.Game.SideToMove)} piece to move.";
            PublishState();
            return;
        }

        if (_isOnlineGame && _onlinePlayerColor is ChessColor playerColor && boardPiece.Color != playerColor)
        {
            _messageText = $"You are playing {FormatColor(playerColor)} in this online room.";
            PublishState();
            return;
        }

        var legalMoves = _session.Game.GetLegalMoves(position);
        if (legalMoves.Count == 0)
        {
            _messageText = $"{boardPiece} on {position} has no legal moves.";
            PublishState();
            return;
        }

        _selectedSquare = position;
        _messageText = $"Selected {boardPiece} on {position}.";
        PublishState();
    }

    private async ValueTask RunComputerTurnIfNeededAsync(CancellationToken cancellationToken)
    {
        if (!IsComputerTurn())
        {
            return;
        }

        try
        {
            _isBusy = true;
            _messageText = $"Computer ({_selectedDifficulty}) is thinking…";
            PublishState();

            if (!_runtimeInitialized)
            {
                await _runtimeBootstrapper.WarmUpAsync(cancellationToken);
                _runtimeInitialized = true;
            }

            var computerMove = await _computerMoveService.GetMoveAsync(_session, _selectedDifficulty, cancellationToken);
            var result = _sessionService.SubmitMove(_session, computerMove);
            _isBusy = false;
            ApplyMoveResult(result, $"Computer played {computerMove}.");
        }
        catch (OperationCanceledException)
        {
            _isBusy = false;
            PublishState();
            throw;
        }
        catch (Exception exception)
        {
            _isBusy = false;
            _messageText = $"Computer move failed: {exception.Message}";
            PublishState();
        }
    }

    private void ApplyMoveResult(SessionMoveResult result, string? appliedMessage)
    {
        _session = result.Session;

        switch (result.Outcome)
        {
            case GameSessionMoveOutcome.Applied:
                _selectedSquare = null;
                _messageText = appliedMessage ?? "Move applied.";
                break;

            case GameSessionMoveOutcome.PromotionSelectionRequired:
                _selectedSquare = null;
                _messageText = $"Choose a promotion piece for {_session.PendingPromotion?.CoordinateNotation}.";
                break;

            case GameSessionMoveOutcome.IllegalMove:
            case GameSessionMoveOutcome.GameAlreadyFinished:
            case GameSessionMoveOutcome.PendingPromotionSelectionRequired:
                _messageText = result.RejectionReason ?? "Move could not be applied.";
                break;

            default:
                _messageText = "Move state updated.";
                break;
        }

        PublishState();
    }

    private void PublishState()
    {
        State = BuildState();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private GameplayViewState BuildState()
    {
        var selectedTargets = _selectedSquare is ChessPosition selectedSquare
            ? _session.Game.GetLegalMoves(selectedSquare).Select(move => move.To).Distinct().ToHashSet()
            : new HashSet<ChessPosition>();

        var squares = new List<ChessBoardSquareViewState>(64);
        for (var displayRow = 0; displayRow < 8; displayRow++)
        {
            for (var displayColumn = 0; displayColumn < 8; displayColumn++)
            {
                var position = TranslateDisplayCoordinates(displayRow, displayColumn, _boardPerspective);
                var piece = _session.Game.Board[position];
                squares.Add(new ChessBoardSquareViewState(
                    position,
                    displayRow,
                    displayColumn,
                    piece,
                    PieceVisualCatalog.GetGlyph(piece),
                    PieceVisualCatalog.GetAssetKey(piece),
                    IsLightSquare(position),
                    _selectedSquare == position,
                    selectedTargets.Contains(position)));
            }
        }

        var pendingPromotion = _session.PendingPromotion;
        return new GameplayViewState(
            DescribeMode(),
            DescribeStatus(),
            _messageText,
            _saveDraft,
            DescribeAiRuntime(),
            _boardPerspective,
            _session.Game.SideToMove,
            _selectedDifficulty,
            _computerPlayerColor,
            _isBusy,
            IsComputerTurn(),
            CanInteractWithBoard(),
            CanChoosePromotion(),
            squares,
            _session.MoveHistory.Select(record => record.DisplayText).ToArray(),
            pendingPromotion?.AvailablePieceTypes.ToArray() ?? Array.Empty<PieceType>(),
            pendingPromotion is null
                ? string.Empty
                : $"Choose a promotion piece for {pendingPromotion.CoordinateNotation}.",
            new OnlinePlayViewState(
                _isOnlineGame,
                _onlineServerUrl,
                _onlinePlayerName,
                _onlineRoomCode,
                _onlinePlayerToken,
                _onlineConnectionSummary,
                DescribeOnlineSeatSummary(),
                _onlinePlayerColor,
                _onlineRoomState?.IsAwaitingOpponent ?? false,
                _onlineIsConnected,
                _onlineIsReconnecting));
    }

    private string DescribeMode()
    {
        if (_isOnlineGame)
        {
            var playerColor = _onlinePlayerColor is ChessColor color ? FormatColor(color) : "Observer";
            var roomCode = string.IsNullOrWhiteSpace(_onlineRoomCode) ? "pending" : _onlineRoomCode;
            return $"Mode: Online room {roomCode} · {playerColor}";
        }

        if (_computerPlayerColor is null)
        {
            return "Mode: Local two-player";
        }

        return $"Mode: vs AI ({_selectedDifficulty}) · Human {FormatColor(GetOpponent(_computerPlayerColor.Value))}";
    }

    private string DescribeStatus()
    {
        if (_isOnlineGame && IsWaitingForOnlineOpponent())
        {
            return "Waiting for Black to join the online room.";
        }

        if (_session.PendingPromotion is PendingPromotionSelection pendingPromotion)
        {
            return $"{FormatColor(pendingPromotion.Player)} promotion pending on {pendingPromotion.CoordinateNotation}.";
        }

        return _session.Game.GetStatus() switch
        {
            ChessGameStatus.InProgress => $"{FormatColor(_session.Game.SideToMove)} to move.",
            ChessGameStatus.Check => $"{FormatColor(_session.Game.SideToMove)} to move and in check.",
            ChessGameStatus.Checkmate => $"Checkmate. {FormatColor(GetOpponent(_session.Game.SideToMove))} wins.",
            ChessGameStatus.Stalemate => "Stalemate.",
            _ => "Game state unavailable."
        };
    }

    private string DescribeAiRuntime()
    {
        return $"AI runtime: {_runtimeDescriptor.IntegrationMode} @ {_runtimeDescriptor.RuntimeLocation}";
    }

    private string DescribeOnlineSeatSummary()
    {
        if (_onlineRoomState is null)
        {
            return "No online room connected.";
        }

        var blackPlayer = string.IsNullOrWhiteSpace(_onlineRoomState.BlackPlayerName)
            ? "Open seat"
            : _onlineRoomState.BlackPlayerName;
        return $"White: {_onlineRoomState.WhitePlayerName} · Black: {blackPlayer}";
    }

    private bool CanInteractWithBoard()
    {
        if (_isBusy || _session.PendingPromotion is not null)
        {
            return false;
        }

        if (_isOnlineGame)
        {
            return _onlineRoomState?.IsAwaitingOpponent == false &&
                   _onlineIsConnected &&
                   _onlinePlayerColor is ChessColor playerColor &&
                   playerColor == _session.Game.SideToMove;
        }

        return !IsComputerTurn();
    }

    private bool CanChoosePromotion()
    {
        if (_isBusy || _session.PendingPromotion is null)
        {
            return false;
        }

        if (!_isOnlineGame)
        {
            return true;
        }

        return _onlineIsConnected &&
               _onlinePlayerColor is ChessColor playerColor &&
               playerColor == _session.PendingPromotion.Player;
    }

    private bool IsComputerTurn()
    {
        return _computerPlayerColor is ChessColor computerColor &&
               !_session.IsComplete &&
               _session.PendingPromotion is null &&
               computerColor == _session.Game.SideToMove;
    }

    private bool IsRemoteOpponentTurn()
    {
        return _isOnlineGame &&
               _onlinePlayerColor is ChessColor playerColor &&
               _session.PendingPromotion is null &&
               _onlineRoomState?.IsAwaitingOpponent == false &&
               playerColor != _session.Game.SideToMove;
    }

    private bool IsWaitingForOnlineOpponent()
    {
        return _isOnlineGame && _onlineRoomState?.IsAwaitingOpponent == true;
    }

    private Uri ParseOnlineServerUri(bool requirePlayerName)
    {
        if (string.IsNullOrWhiteSpace(_onlineServerUrl))
        {
            throw new InvalidOperationException("Enter the multiplayer backend URL first.");
        }

        if (!Uri.TryCreate(_onlineServerUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Enter an absolute http:// or https:// backend URL.");
        }

        if (requirePlayerName && string.IsNullOrWhiteSpace(_onlinePlayerName))
        {
            throw new InvalidOperationException("Enter a player name before creating or joining a room.");
        }

        return uri;
    }

    private void OnOnlineRoomStateChanged(OnlineGameRoomState roomState)
    {
        try
        {
            ApplyOnlineRoomState(roomState);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            _messageText = $"Online state sync failed: {exception.Message}";
            PublishState();
        }
    }

    private void OnOnlineConnectionStatusChanged(OnlineConnectionStatus status)
    {
        _onlineConnectionSummary = status.Summary;
        _onlineIsConnected = status.IsConnected;
        _onlineIsReconnecting = status.IsReconnecting;
        PublishState();
    }

    private static ChessPosition TranslateDisplayCoordinates(int displayRow, int displayColumn, ChessColor perspective)
    {
        return perspective == ChessColor.White
            ? new ChessPosition(displayColumn, 7 - displayRow)
            : new ChessPosition(7 - displayColumn, displayRow);
    }

    private static bool IsLightSquare(ChessPosition position)
    {
        return (position.File + position.Rank) % 2 != 0;
    }

    private static ChessColor GetOpponent(ChessColor color)
    {
        return color == ChessColor.White ? ChessColor.Black : ChessColor.White;
    }

    private static string FormatColor(ChessColor color)
    {
        return color == ChessColor.White ? "White" : "Black";
    }

    private static string FormatPieceType(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => "pawn",
            PieceType.Knight => "knight",
            PieceType.Bishop => "bishop",
            PieceType.Rook => "rook",
            PieceType.Queen => "queen",
            PieceType.King => "king",
            _ => pieceType.ToString()
        };
    }
}

public sealed record GameplayViewState(
    string ModeSummary,
    string StatusText,
    string MessageText,
    string SaveDraft,
    string AiRuntimeSummary,
    ChessColor BoardPerspective,
    ChessColor SideToMove,
    AiDifficulty SelectedDifficulty,
    ChessColor? ComputerPlayerColor,
    bool IsBusy,
    bool IsComputerTurn,
    bool CanInteractWithBoard,
    bool CanChoosePromotion,
    IReadOnlyList<ChessBoardSquareViewState> BoardSquares,
    IReadOnlyList<string> MoveHistory,
    IReadOnlyList<PieceType> PendingPromotionChoices,
    string PendingPromotionPrompt,
    OnlinePlayViewState OnlinePlay)
{
    public bool HasPendingPromotion => PendingPromotionChoices.Count > 0;
}

public sealed record OnlinePlayViewState(
    bool IsOnlineGame,
    string ServerUrl,
    string PlayerName,
    string RoomCode,
    string PlayerToken,
    string ConnectionSummary,
    string SeatSummary,
    ChessColor? PlayerColor,
    bool IsAwaitingOpponent,
    bool IsConnected,
    bool IsReconnecting);

public sealed record ChessBoardSquareViewState(
    ChessPosition Position,
    int DisplayRow,
    int DisplayColumn,
    ChessPiece? Piece,
    string PieceGlyph,
    string PieceAssetKey,
    bool IsLightSquare,
    bool IsSelected,
    bool IsLegalDestination);

internal static class PieceVisualCatalog
{
    public static string GetGlyph(ChessPiece? piece)
    {
        return piece switch
        {
            { Color: ChessColor.White, Type: PieceType.Pawn } => "♙",
            { Color: ChessColor.White, Type: PieceType.Knight } => "♘",
            { Color: ChessColor.White, Type: PieceType.Bishop } => "♗",
            { Color: ChessColor.White, Type: PieceType.Rook } => "♖",
            { Color: ChessColor.White, Type: PieceType.Queen } => "♕",
            { Color: ChessColor.White, Type: PieceType.King } => "♔",
            { Color: ChessColor.Black, Type: PieceType.Pawn } => "♟",
            { Color: ChessColor.Black, Type: PieceType.Knight } => "♞",
            { Color: ChessColor.Black, Type: PieceType.Bishop } => "♝",
            { Color: ChessColor.Black, Type: PieceType.Rook } => "♜",
            { Color: ChessColor.Black, Type: PieceType.Queen } => "♛",
            { Color: ChessColor.Black, Type: PieceType.King } => "♚",
            _ => string.Empty
        };
    }

    public static string GetAssetKey(ChessPiece? piece)
    {
        return piece switch
        {
            { Color: ChessColor.White, Type: PieceType.Pawn } => "white-pawn",
            { Color: ChessColor.White, Type: PieceType.Knight } => "white-knight",
            { Color: ChessColor.White, Type: PieceType.Bishop } => "white-bishop",
            { Color: ChessColor.White, Type: PieceType.Rook } => "white-rook",
            { Color: ChessColor.White, Type: PieceType.Queen } => "white-queen",
            { Color: ChessColor.White, Type: PieceType.King } => "white-king",
            { Color: ChessColor.Black, Type: PieceType.Pawn } => "black-pawn",
            { Color: ChessColor.Black, Type: PieceType.Knight } => "black-knight",
            { Color: ChessColor.Black, Type: PieceType.Bishop } => "black-bishop",
            { Color: ChessColor.Black, Type: PieceType.Rook } => "black-rook",
            { Color: ChessColor.Black, Type: PieceType.Queen } => "black-queen",
            { Color: ChessColor.Black, Type: PieceType.King } => "black-king",
            _ => string.Empty
        };
    }
}