using NiteChess.Application.Configuration;

namespace NiteChess.Desktop;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(NiteChessBootstrapManifest manifest)
    {
        Title = "NiteChess Desktop";
        Summary = $"{manifest.Platform.Surface} scaffold ready. " +
                  $"Offline AI seam: {manifest.Stockfish.IntegrationMode}. " +
                  "Future gameplay UI and Stockfish process wiring are intentionally deferred.";
    }

    public string Title { get; }

    public string Summary { get; }
}
