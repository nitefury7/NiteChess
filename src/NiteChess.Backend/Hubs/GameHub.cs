using Microsoft.AspNetCore.SignalR;
using NiteChess.Backend.Services;
using NiteChess.Online.Contracts;

namespace NiteChess.Backend.Hubs;

public sealed class GameHub : Hub<IGameClient>
{
    private readonly OnlineGameRoomService _roomService;

    public GameHub(OnlineGameRoomService roomService)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _roomService.ReleaseConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task Ping()
    {
        return Clients.Caller.ServerHeartbeat(new ServerHeartbeat("backend", DateTimeOffset.UtcNow));
    }

    public async Task<OnlineRoomConnectionResult> CreateRoom(CreateRoomRequest request)
    {
        await RemoveConnectionFromPreviousRoomAsync();
        var result = _roomService.CreateRoom(Context.ConnectionId, request.PlayerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomCode);
        await Clients.OthersInGroup(result.RoomCode).RoomStateUpdated(result.RoomState);
        return result;
    }

    public async Task<OnlineRoomConnectionResult> JoinRoom(JoinRoomRequest request)
    {
        await RemoveConnectionFromPreviousRoomAsync();
        var result = _roomService.JoinRoom(Context.ConnectionId, request.RoomCode, request.PlayerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomCode);
        await Clients.OthersInGroup(result.RoomCode).RoomStateUpdated(result.RoomState);
        return result;
    }

    public async Task<OnlineRoomConnectionResult> ResumeRoom(ResumeRoomRequest request)
    {
        await RemoveConnectionFromPreviousRoomAsync();
        var result = _roomService.ResumeRoom(Context.ConnectionId, request.RoomCode, request.PlayerToken);
        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomCode);
        await Clients.OthersInGroup(result.RoomCode).RoomStateUpdated(result.RoomState);
        return result;
    }

    public async Task<OnlineGameActionResult> SubmitMove(OnlineMoveRequest request)
    {
        var result = _roomService.SubmitMove(Context.ConnectionId, request.From, request.To);
        if (result.Accepted)
        {
            await Clients.OthersInGroup(result.RoomState.RoomCode).RoomStateUpdated(result.RoomState);
        }

        return result;
    }

    public async Task<OnlineGameActionResult> CompletePromotion(OnlinePromotionRequest request)
    {
        var result = _roomService.CompletePromotion(Context.ConnectionId, request.PromotionPieceType);
        if (result.Accepted)
        {
            await Clients.OthersInGroup(result.RoomState.RoomCode).RoomStateUpdated(result.RoomState);
        }

        return result;
    }

    private async Task RemoveConnectionFromPreviousRoomAsync()
    {
        var previousRoomCode = _roomService.ReleaseConnection(Context.ConnectionId);
        if (!string.IsNullOrWhiteSpace(previousRoomCode))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousRoomCode);
        }
    }
}
