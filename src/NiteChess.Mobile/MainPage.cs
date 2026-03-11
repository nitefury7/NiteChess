using NiteChess.Application.Configuration;

namespace NiteChess.Mobile;

public sealed class MainPage : ContentPage
{
    public MainPage(NiteChessBootstrapManifest manifest)
    {
        Title = "NiteChess Mobile";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "NiteChess Mobile Scaffold",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"{manifest.Platform.Surface} shell is ready for later gameplay UI work."
                    },
                    new Label
                    {
                        Text = $"Offline AI seam: {manifest.Stockfish.IntegrationMode} ({manifest.Stockfish.RuntimeLocation})"
                    },
                    new Label
                    {
                        Text = "Future iOS/Android gameplay screens, persistence, and engine packaging are intentionally deferred."
                    }
                }
            }
        };
    }
}
