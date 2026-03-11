using NiteChess.Application.Configuration;
using NiteChess.Application.DependencyInjection;
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
        RuntimeLocation: "wwwroot/workers/stockfish.worker.js",
        IsBundled: false,
        Notes: "Bundle manifest ships at wwwroot/stockfish/web-stockfish.bundle.json; add Stockfish 18 WASM assets under wwwroot/stockfish and boot them through the dedicated worker."));
builder.Services.AddSingleton<IStockfishRuntimeBootstrapper, BrowserWorkerStockfishRuntimeBootstrapper>();
builder.Services.AddSingleton<IStockfishEngineClient, BrowserWorkerStockfishEngineClient>();

await builder.Build().RunAsync();
