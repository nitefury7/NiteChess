# NiteChess

NiteChess is a .NET 10 cross-platform chess application with shared chess logic, offline Stockfish-powered AI, local save/load, and online multiplayer via ASP.NET Core + SignalR across desktop, mobile, and web surfaces.

## Solution layout

- `src/NiteChess.Domain` - chess rules and board-state logic.
- `src/NiteChess.Application` - shared gameplay/session orchestration and bootstrap manifests.
- `src/NiteChess.Stockfish` - Stockfish runtime abstractions and host-specific resolution helpers.
- `src/NiteChess.Online` - shared SignalR client/contracts layer.
- `src/NiteChess.Backend` - ASP.NET Core multiplayer backend.
- `src/NiteChess.Web.Client` - Blazor WebAssembly client.
- `src/NiteChess.Web.Host` - ASP.NET Core host for the Blazor client static assets.
- `src/NiteChess.Desktop` - Avalonia desktop shell.
- `src/NiteChess.Mobile` - .NET MAUI mobile shell.
- `tests/NiteChess.Domain.Scenarios` - scenario-style verification runner.

## Prerequisites

- .NET 10 SDK for restore/build/run.
- .NET MAUI workload for mobile builds (`dotnet workload install maui`).
- Platform toolchains for the targets you build:
  - Android SDK/emulator or device for `net10.0-android`.
  - Xcode/iOS tooling for `net10.0-ios`.
  - A supported desktop runtime for Avalonia on macOS or Linux.

## Restore and build

1. Restore the solution:
   - `dotnet restore NiteChess.sln`
2. Build everything that the current machine supports:
   - `dotnet build NiteChess.sln`
3. Run the scenario verification executable:
   - `dotnet run --project tests/NiteChess.Domain.Scenarios`

If MAUI workloads or platform SDKs are unavailable, desktop/backend/web builds may still succeed while mobile builds remain blocked until those toolchains are installed.

## Run targets

### Backend

- Start the multiplayer server with `dotnet run --project src/NiteChess.Backend`.
- Health endpoint: `/health`
- SignalR hub route: `/hubs/game`
- Desktop, mobile, and web multiplayer clients expect an absolute backend URL such as `https://localhost:5001`.

### Web

- Start the hosted browser app with `dotnet run --project src/NiteChess.Web.Host`.
- Use the host project as the primary run path for release-like validation so the Blazor assets, Stockfish files, and worker are served together.
- Offline AI stays in-browser once the app bundle and Stockfish assets are loaded.
- Online multiplayer still requires the separate backend process from `src/NiteChess.Backend`.

### Desktop

- Start the Avalonia app with `dotnet run --project src/NiteChess.Desktop`.
- Desktop ships bundled Stockfish assets under `src/NiteChess.Desktop/Assets/Stockfish` and copies them to the app output.
- Online multiplayer uses the backend URL entered in the desktop UI.

### Mobile

- Android build: `dotnet build src/NiteChess.Mobile/NiteChess.Mobile.csproj -f net10.0-android`
- iOS build: `dotnet build src/NiteChess.Mobile/NiteChess.Mobile.csproj -f net10.0-ios`
- The MAUI app is a single project and requires the matching platform SDK/toolchain to run on a device or simulator.
- Online multiplayer uses the backend URL entered in the mobile UI.

## Stockfish runtime expectations by target

### Desktop

- Manifest: `src/NiteChess.Desktop/Assets/Stockfish/desktop-stockfish.bundle.json`
- Integration mode: native process
- Bundled payloads currently checked in:
  - `native/osx-arm64/stockfish`
  - `native/osx-x64/stockfish`
  - `native/linux-x64/stockfish`
- Caveat: the repo contains a reserved `native/linux-arm64` directory, but no Linux arm64 binary is currently bundled. Add one before advertising Linux arm64 offline-AI support.

### Mobile

- Manifest: `src/NiteChess.Mobile/Resources/Raw/Stockfish/mobile-stockfish.bundle.json`
- Integration mode: native library/runtime bridge
- Bundled payloads currently checked in:
  - Android arm64 executable: `native/android-arm64-v8a/stockfish`
  - iOS arm64 bridge library: `native/ios-arm64/libnitechess_stockfish_bridge.a`
- Caveat: Android `x86_64` emulator payloads are not currently bundled in the repo, so offline AI should be validated on arm64 hardware/images unless an additional `android-x86_64` runtime is added.

### Web

- Manifest: `src/NiteChess.Web.Client/wwwroot/stockfish/web-stockfish.bundle.json`
- Integration mode: browser WebAssembly worker
- Required runtime assets:
  - `wwwroot/stockfish/stockfish.js`
  - `wwwroot/stockfish/stockfish.wasm`
  - `wwwroot/workers/stockfish.worker.js`
- Caveat: the worker and WASM assets must be served by the web host; offline AI is browser-local, but multiplayer still depends on the separate backend.

### Backend

- Stockfish is intentionally disabled on the backend.
- Offline AI remains client-side by design; the backend only coordinates multiplayer state.

## Target-specific caveats

- The backend currently uses a permissive CORS policy suitable for local cross-surface development; tighten allowed origins before any internet-facing deployment.
- The web host and multiplayer backend are separate ASP.NET Core processes in the current repo layout.
- Desktop/mobile/web multiplayer fields expect an absolute `http://` or `https://` backend URL.
- Full end-to-end validation still requires a machine with the .NET 10 SDK and, for mobile, the MAUI workload plus platform toolchains.

## Recommended final verification pass

When the required SDKs are available, use this sequence:

1. `dotnet restore NiteChess.sln`
2. `dotnet build NiteChess.sln`
3. `dotnet run --project tests/NiteChess.Domain.Scenarios`
4. `dotnet run --project src/NiteChess.Backend` and confirm `/health`
5. `dotnet run --project src/NiteChess.Web.Host` and verify browser local play, offline AI, and backend-backed multiplayer
6. `dotnet run --project src/NiteChess.Desktop` and verify local play, offline AI, and backend-backed multiplayer
7. `dotnet build src/NiteChess.Mobile/NiteChess.Mobile.csproj -f net10.0-android`
8. `dotnet build src/NiteChess.Mobile/NiteChess.Mobile.csproj -f net10.0-ios`