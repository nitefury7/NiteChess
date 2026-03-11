using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
using NiteChess.Backend.Hubs;
using NiteChess.Backend.Services;
using NiteChess.Online.Contracts;
using NiteChess.Stockfish.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNiteChessApplication(
    new NiteChessPlatformDescriptor(
        HostId: "backend",
        Surface: "AspNetCoreSignalR",
        SupportsOfflineAi: false,
        SupportsOnlinePlay: true,
        Notes: "Backend is reserved for online multiplayer coordination and APIs."),
    new StockfishRuntimeDescriptor(
        HostId: "backend",
        IntegrationMode: StockfishIntegrationMode.Disabled,
        RuntimeLocation: "n/a",
        IsBundled: false,
        Notes: "Offline Stockfish stays client-side by design."));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<OnlineGameRoomService>();

var app = builder.Build();

app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "ok", host = "backend" }));
app.MapHub<GameHub>(GameHubRoutes.GameHub);

app.Run();
