using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace NiteChess.Desktop;

public sealed class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        Title = viewModel.Title;
        Width = 720;
        Height = 420;

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = viewModel.Title,
                    FontSize = 24,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = viewModel.Summary,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            }
        };
    }
}
