# NiteChess

Wave 1 scaffolds the shared .NET 10 solution architecture for a cross-platform chess application.

## Solution layout

- `src/NiteChess.Domain` – future chess and game-state domain layer.
- `src/NiteChess.Application` – shared dependency injection and bootstrap manifest wiring.
- `src/NiteChess.Stockfish` – Stockfish integration contracts for native and browser-hosted runtimes.
- `src/NiteChess.Online` – shared online multiplayer/SignalR contracts.
- `src/NiteChess.Backend` – ASP.NET Core + SignalR backend.
- `src/NiteChess.Web.Client` – Blazor WebAssembly client.
- `src/NiteChess.Web.Host` – ASP.NET Core host for the Blazor WebAssembly client.
- `src/NiteChess.Desktop` – Avalonia desktop shell.
- `src/NiteChess.Mobile` – .NET MAUI mobile shell.

## Prerequisites

- .NET 10 SDK
- .NET MAUI workload for mobile builds
- Platform toolchains for the selected MAUI targets

## Getting started

1. Restore: `dotnet restore`
2. Build: `dotnet build`
3. Smoke-test the hosts you want to work on next:
   - `dotnet run --project src/NiteChess.Backend`
   - `dotnet run --project src/NiteChess.Web.Host`
   - `dotnet run --project src/NiteChess.Web.Client`
   - `dotnet run --project src/NiteChess.Desktop`

This milestone intentionally stops at scaffold/buildability and initial architecture seams. Chess rules, gameplay flows, persistence, and full Stockfish integration are deferred to later waves.