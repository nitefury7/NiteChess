using NiteChess.Domain.Chess;
using NiteChess.Online.Contracts;

namespace NiteChess.Online;

public sealed class DisabledOnlineGameClient : IOnlineGameClient
{
    public event Action<OnlineGameRoomState>? RoomStateChanged;

    public event Action<OnlineConnectionStatus>? ConnectionStatusChanged;

    public ValueTask<OnlineRoomConnectionResult> CreateRoomAsync(Uri serverUri, string playerName, CancellationToken cancellationToken = default)
    {
        throw CreateUnavailableException();
    }

    public ValueTask<OnlineRoomConnectionResult> JoinRoomAsync(Uri serverUri, string roomCode, string playerName, CancellationToken cancellationToken = default)
    {
        throw CreateUnavailableException();
    }

    public ValueTask<OnlineRoomConnectionResult> ResumeRoomAsync(Uri serverUri, string roomCode, string playerToken, CancellationToken cancellationToken = default)
    {
        throw CreateUnavailableException();
    }

    public ValueTask<OnlineGameActionResult> SubmitMoveAsync(string from, string to, CancellationToken cancellationToken = default)
    {
        throw CreateUnavailableException();
    }

    public ValueTask<OnlineGameActionResult> CompletePromotionAsync(PieceType promotionPieceType, CancellationToken cancellationToken = default)
    {
        throw CreateUnavailableException();
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    private static InvalidOperationException CreateUnavailableException()
    {
        return new InvalidOperationException("Online multiplayer is not configured for this host.");
    }
}