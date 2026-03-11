using NiteChess.Domain.Chess;
using NiteChess.Online.Contracts;

namespace NiteChess.Online;

public interface IOnlineGameClient
{
    event Action<OnlineGameRoomState>? RoomStateChanged;

    event Action<OnlineConnectionStatus>? ConnectionStatusChanged;

    ValueTask<OnlineRoomConnectionResult> CreateRoomAsync(Uri serverUri, string playerName, CancellationToken cancellationToken = default);

    ValueTask<OnlineRoomConnectionResult> JoinRoomAsync(Uri serverUri, string roomCode, string playerName, CancellationToken cancellationToken = default);

    ValueTask<OnlineRoomConnectionResult> ResumeRoomAsync(Uri serverUri, string roomCode, string playerToken, CancellationToken cancellationToken = default);

    ValueTask<OnlineGameActionResult> SubmitMoveAsync(string from, string to, CancellationToken cancellationToken = default);

    ValueTask<OnlineGameActionResult> CompletePromotionAsync(PieceType promotionPieceType, CancellationToken cancellationToken = default);

    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
}

public sealed record OnlineConnectionStatus(string Summary, bool IsConnected, bool IsReconnecting);