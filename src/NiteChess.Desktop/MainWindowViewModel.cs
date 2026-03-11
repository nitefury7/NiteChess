using NiteChess.Application.Gameplay;
using NiteChess.Application.Configuration;

namespace NiteChess.Desktop;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(NiteChessBootstrapManifest manifest, GameplayController gameplay)
    {
        Title = "NiteChess Desktop";
        Subtitle = $"{manifest.Platform.Surface} board play, history, save/load, and offline AI.";
        Gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
    }

    public string Title { get; }

    public string Subtitle { get; }

    public GameplayController Gameplay { get; }
}
