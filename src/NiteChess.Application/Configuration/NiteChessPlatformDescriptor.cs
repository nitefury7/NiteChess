namespace NiteChess.Application.Configuration;

public sealed record NiteChessPlatformDescriptor(
    string HostId,
    string Surface,
    bool SupportsOfflineAi,
    bool SupportsOnlinePlay,
    string Notes);
