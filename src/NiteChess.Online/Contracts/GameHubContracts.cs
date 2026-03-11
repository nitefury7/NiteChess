namespace NiteChess.Online.Contracts;

public static class GameHubRoutes
{
    public const string GameHub = "/hubs/game";
}

public sealed record ServerHeartbeat(string Source, DateTimeOffset Timestamp);

public interface IGameClient
{
    Task ServerHeartbeat(ServerHeartbeat heartbeat);
}
