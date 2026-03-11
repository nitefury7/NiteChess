using NiteChess.Domain.Chess;

namespace NiteChess.Online.Contracts;

public static class GameHubRoutes
{
    public const string GameHub = "/hubs/game";
}

public static class GameHubMethods
{
    public const string CreateRoom = "CreateRoom";
    public const string JoinRoom = "JoinRoom";
    public const string ResumeRoom = "ResumeRoom";
    public const string SubmitMove = "SubmitMove";
    public const string CompletePromotion = "CompletePromotion";
    public const string Ping = "Ping";
}

public sealed record ServerHeartbeat(string Source, DateTimeOffset Timestamp);

public sealed record CreateRoomRequest(string PlayerName);

public sealed record JoinRoomRequest(string RoomCode, string PlayerName);

public sealed record ResumeRoomRequest(string RoomCode, string PlayerToken);

public sealed record OnlineMoveRequest(string From, string To);

public sealed record OnlinePromotionRequest(PieceType PromotionPieceType);

public sealed record OnlineGameRoomState(
    string RoomCode,
    string SessionSnapshot,
    string WhitePlayerName,
    string? BlackPlayerName,
    bool IsAwaitingOpponent,
    string StatusMessage,
    DateTimeOffset UpdatedAt);

public sealed record OnlineRoomConnectionResult(
    string RoomCode,
    string PlayerToken,
    string PlayerName,
    ChessColor PlayerColor,
    OnlineGameRoomState RoomState);

public sealed record OnlineGameActionResult(
    bool Accepted,
    string Message,
    OnlineGameRoomState RoomState);

public interface IGameClient
{
    Task ServerHeartbeat(ServerHeartbeat heartbeat);

    Task RoomStateUpdated(OnlineGameRoomState roomState);
}
