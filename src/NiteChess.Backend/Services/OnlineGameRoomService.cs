using NiteChess.Application.GameSessions;
using NiteChess.Domain.Chess;
using NiteChess.Online.Contracts;

namespace NiteChess.Backend.Services;

public sealed class OnlineGameRoomService
{
    private static readonly char[] RoomCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private readonly object _gate = new();
    private readonly IGameSessionService _sessionService;
    private readonly IGameSessionPersistenceService _persistenceService;
    private readonly Dictionary<string, OnlineGameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConnectionBinding> _connections = new(StringComparer.Ordinal);

    public OnlineGameRoomService(IGameSessionService sessionService, IGameSessionPersistenceService persistenceService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
    }

    public string? ReleaseConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return null;
        }

        lock (_gate)
        {
            if (_connections.Remove(connectionId, out var binding))
            {
                return binding.RoomCode;
            }

            return null;
        }
    }

    public OnlineRoomConnectionResult CreateRoom(string connectionId, string playerName)
    {
        lock (_gate)
        {
            var roomCode = GenerateRoomCode();
            var room = new OnlineGameRoom(
                roomCode,
                _sessionService.CreateSession(),
                new OnlinePlayerSeat(ChessColor.White, NormalizePlayerName(playerName), CreatePlayerToken()));

            room.StatusMessage = $"Room {roomCode} created. Share the code so Black can join.";
            room.UpdatedAt = DateTimeOffset.UtcNow;
            _rooms.Add(roomCode, room);
            RebindConnection(connectionId, roomCode, room.White.PlayerToken);
            return BuildConnectionResult(room, room.White);
        }
    }

    public OnlineRoomConnectionResult JoinRoom(string connectionId, string roomCode, string playerName)
    {
        lock (_gate)
        {
            var room = GetRoom(roomCode);
            if (room.Black is not null)
            {
                throw new InvalidOperationException($"Room '{room.RoomCode}' already has two players.");
            }

            room.Black = new OnlinePlayerSeat(ChessColor.Black, NormalizePlayerName(playerName), CreatePlayerToken());
            room.StatusMessage = $"{room.Black.PlayerName} joined as Black. {DescribeTurn(room.Session)}";
            room.UpdatedAt = DateTimeOffset.UtcNow;
            RebindConnection(connectionId, room.RoomCode, room.Black.PlayerToken);
            return BuildConnectionResult(room, room.Black);
        }
    }

    public OnlineRoomConnectionResult ResumeRoom(string connectionId, string roomCode, string playerToken)
    {
        lock (_gate)
        {
            var room = GetRoom(roomCode);
            var seat = room.ResolveSeat(NormalizePlayerToken(playerToken))
                ?? throw new InvalidOperationException("Reconnect token does not match any player in this room.");

            room.StatusMessage = $"{seat.PlayerName} reconnected as {FormatColor(seat.PlayerColor)}. {DescribeTurn(room.Session)}";
            room.UpdatedAt = DateTimeOffset.UtcNow;
            RebindConnection(connectionId, room.RoomCode, seat.PlayerToken);
            return BuildConnectionResult(room, seat);
        }
    }

    public OnlineGameActionResult SubmitMove(string connectionId, string from, string to)
    {
        lock (_gate)
        {
            var (room, seat) = ResolveBoundSeat(connectionId);
            if (room.Black is null)
            {
                return Reject(room, "Waiting for an opponent to join before moves can begin.");
            }

            if (room.Session.Game.SideToMove != seat.PlayerColor)
            {
                return Reject(room, $"It is {FormatColor(room.Session.Game.SideToMove)}'s turn.");
            }

            var result = _sessionService.SubmitMove(
                room.Session,
                ChessPosition.Parse(from),
                ChessPosition.Parse(to));

            if (result.Outcome is not (GameSessionMoveOutcome.Applied or GameSessionMoveOutcome.PromotionSelectionRequired))
            {
                return Reject(room, result.RejectionReason ?? "Move rejected by the server.");
            }

            room.Session = result.Session;
            room.UpdatedAt = DateTimeOffset.UtcNow;
            room.StatusMessage = result.Outcome == GameSessionMoveOutcome.PromotionSelectionRequired
                ? $"{seat.PlayerName} must choose a promotion piece for {room.Session.PendingPromotion?.CoordinateNotation}."
                : $"{seat.PlayerName} played {result.AppliedMove?.DisplayText}. {DescribeTurn(room.Session)}";

            return Accept(room, room.StatusMessage);
        }
    }

    public OnlineGameActionResult CompletePromotion(string connectionId, PieceType promotionPieceType)
    {
        lock (_gate)
        {
            var (room, seat) = ResolveBoundSeat(connectionId);
            if (room.Session.PendingPromotion is null)
            {
                return Reject(room, "No promotion selection is currently pending.");
            }

            if (room.Session.PendingPromotion.Player != seat.PlayerColor)
            {
                return Reject(room, $"Waiting for {FormatColor(room.Session.PendingPromotion.Player)} to choose the promotion piece.");
            }

            var result = _sessionService.CompletePromotion(room.Session, promotionPieceType);
            if (result.Outcome != GameSessionMoveOutcome.Applied)
            {
                return Reject(room, result.RejectionReason ?? "Promotion selection was rejected by the server.");
            }

            room.Session = result.Session;
            room.UpdatedAt = DateTimeOffset.UtcNow;
            room.StatusMessage = $"{seat.PlayerName} promoted to {promotionPieceType}. {DescribeTurn(room.Session)}";
            return Accept(room, room.StatusMessage);
        }
    }

    private OnlineGameActionResult Accept(OnlineGameRoom room, string message)
    {
        room.StatusMessage = message;
        return new OnlineGameActionResult(true, message, BuildRoomState(room));
    }

    private OnlineGameActionResult Reject(OnlineGameRoom room, string message)
    {
        return new OnlineGameActionResult(false, message, BuildRoomState(room));
    }

    private OnlineRoomConnectionResult BuildConnectionResult(OnlineGameRoom room, OnlinePlayerSeat seat)
    {
        return new OnlineRoomConnectionResult(
            room.RoomCode,
            seat.PlayerToken,
            seat.PlayerName,
            seat.PlayerColor,
            BuildRoomState(room));
    }

    private OnlineGameRoomState BuildRoomState(OnlineGameRoom room)
    {
        return new OnlineGameRoomState(
            room.RoomCode,
            _persistenceService.Save(room.Session),
            room.White.PlayerName,
            room.Black?.PlayerName,
            room.Black is null,
            room.StatusMessage,
            room.UpdatedAt);
    }

    private (OnlineGameRoom Room, OnlinePlayerSeat Seat) ResolveBoundSeat(string connectionId)
    {
        if (!_connections.TryGetValue(connectionId, out var binding))
        {
            throw new InvalidOperationException("Reconnect to a room before sending multiplayer commands.");
        }

        var room = GetRoom(binding.RoomCode);
        var seat = room.ResolveSeat(binding.PlayerToken)
            ?? throw new InvalidOperationException("The connection is no longer associated with a valid player seat.");
        return (room, seat);
    }

    private OnlineGameRoom GetRoom(string roomCode)
    {
        var normalized = NormalizeRoomCode(roomCode);
        if (!_rooms.TryGetValue(normalized, out var room))
        {
            throw new InvalidOperationException($"Room '{normalized}' does not exist.");
        }

        return room;
    }

    private void RebindConnection(string connectionId, string roomCode, string playerToken)
    {
        var duplicatedConnections = _connections
            .Where(pair => string.Equals(pair.Value.RoomCode, roomCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pair.Value.PlayerToken, playerToken, StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var duplicatedConnection in duplicatedConnections)
        {
            _connections.Remove(duplicatedConnection);
        }

        _connections[connectionId] = new ConnectionBinding(roomCode, playerToken);
    }

    private string GenerateRoomCode()
    {
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6)
                .Select(_ => RoomCodeAlphabet[Random.Shared.Next(RoomCodeAlphabet.Length)])
                .ToArray());

            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }
    }

    private static string NormalizePlayerName(string playerName)
    {
        var trimmed = playerName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Player name must not be blank.", nameof(playerName));
        }

        return trimmed.Length <= 32 ? trimmed : trimmed[..32];
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var trimmed = roomCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Room code must not be blank.", nameof(roomCode));
        }

        return trimmed;
    }

    private static string NormalizePlayerToken(string playerToken)
    {
        var trimmed = playerToken?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Reconnect token must not be blank.", nameof(playerToken));
        }

        return trimmed;
    }

    private static string CreatePlayerToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string DescribeTurn(LocalGameSession session)
    {
        if (session.PendingPromotion is PendingPromotionSelection pendingPromotion)
        {
            return $"{FormatColor(pendingPromotion.Player)} must finish the promotion on {pendingPromotion.CoordinateNotation}.";
        }

        return session.Game.GetStatus() switch
        {
            ChessGameStatus.InProgress => $"{FormatColor(session.Game.SideToMove)} to move.",
            ChessGameStatus.Check => $"{FormatColor(session.Game.SideToMove)} to move and in check.",
            ChessGameStatus.Checkmate => $"Checkmate. {FormatColor(GetOpponent(session.Game.SideToMove))} wins.",
            ChessGameStatus.Stalemate => "Stalemate.",
            _ => "Game state unavailable."
        };
    }

    private static ChessColor GetOpponent(ChessColor color)
    {
        return color == ChessColor.White ? ChessColor.Black : ChessColor.White;
    }

    private static string FormatColor(ChessColor color)
    {
        return color == ChessColor.White ? "White" : "Black";
    }

    private sealed class OnlineGameRoom
    {
        public OnlineGameRoom(string roomCode, LocalGameSession session, OnlinePlayerSeat white)
        {
            RoomCode = roomCode;
            Session = session;
            White = white;
            UpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = string.Empty;
        }

        public string RoomCode { get; }

        public LocalGameSession Session { get; set; }

        public OnlinePlayerSeat White { get; }

        public OnlinePlayerSeat? Black { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public string StatusMessage { get; set; }

        public OnlinePlayerSeat? ResolveSeat(string playerToken)
        {
            if (string.Equals(White.PlayerToken, playerToken, StringComparison.Ordinal))
            {
                return White;
            }

            if (Black is not null && string.Equals(Black.PlayerToken, playerToken, StringComparison.Ordinal))
            {
                return Black;
            }

            return null;
        }
    }

    private sealed record OnlinePlayerSeat(ChessColor PlayerColor, string PlayerName, string PlayerToken);

    private sealed record ConnectionBinding(string RoomCode, string PlayerToken);
}