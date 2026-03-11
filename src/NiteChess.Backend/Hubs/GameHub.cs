using Microsoft.AspNetCore.SignalR;
using NiteChess.Online.Contracts;

namespace NiteChess.Backend.Hubs;

public sealed class GameHub : Hub<IGameClient>
{
    public Task Ping()
    {
        return Clients.Caller.ServerHeartbeat(new ServerHeartbeat("backend", DateTimeOffset.UtcNow));
    }
}
