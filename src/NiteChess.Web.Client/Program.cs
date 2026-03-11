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
        Notes: "Browser client is prepared for offline AI and hosted deployment."),
    new StockfishRuntimeDescriptor(
        HostId: "web-client",
        IntegrationMode: StockfishIntegrationMode.BrowserWasmWorker,
        RuntimeLocation: "wwwroot/workers/stockfish.worker.js",
        IsBundled: false,
        Notes: "Actual browser worker + WASM asset wiring lands in a later wave."));
builder.Services.AddSingleton<IStockfishRuntimeBootstrapper, BrowserWorkerStockfishRuntimeBootstrapper>();

await builder.Build().RunAsync();
