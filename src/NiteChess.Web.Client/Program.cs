using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
using NiteChess.Application.Gameplay;
using NiteChess.Stockfish.Abstractions;
using NiteChess.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddNiteChessApplication(
    new NiteChessPlatformDescriptor(
        HostId: "web-client",
        Surface: "BlazorWebAssembly",
        SupportsOfflineAi: true,
        SupportsOnlinePlay: true,
        Notes: "Browser client is prepared for offline AI via a Stockfish WebAssembly worker bundle under wwwroot/stockfish."),
    new StockfishRuntimeDescriptor(
        HostId: "web-client",
        IntegrationMode: StockfishIntegrationMode.BrowserWasmWorker,
        RuntimeLocation: "wwwroot/stockfish/web-stockfish.bundle.json",
        IsBundled: true,
        Notes: "Bundle manifest ships at wwwroot/stockfish/web-stockfish.bundle.json and resolves the Stockfish 18 worker plus WASM assets for offline browser play."));
builder.Services.AddSingleton<IStockfishRuntimeBootstrapper, BrowserWorkerStockfishRuntimeBootstrapper>();
builder.Services.AddSingleton<IStockfishEngineClient, BrowserWorkerStockfishEngineClient>();
builder.Services.AddSingleton<GameplayController>();

await builder.Build().RunAsync();
