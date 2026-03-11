using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NiteChess.Application.ComputerPlay;
using NiteChess.Application.Gameplay;
using NiteChess.Domain.Chess;

namespace NiteChess.Desktop;

public sealed class MainWindow : Window
{
    private static readonly IBrush LightSquareBrush = CreateBrush("#EEEED2");
    private static readonly IBrush DarkSquareBrush = CreateBrush("#769656");
    private static readonly IBrush SelectedSquareBrush = CreateBrush("#F6F669");
    private static readonly IBrush LegalTargetBrush = CreateBrush("#BACA44");
    private static readonly IBrush WhitePieceBrush = CreateBrush("#F9FAFB");
    private static readonly IBrush BlackPieceBrush = CreateBrush("#111827");
    private static readonly IBrush MutedTextBrush = CreateBrush("#475569");

    private readonly MainWindowViewModel _viewModel;
    private readonly GameplayController _gameplay;
    private readonly TextBlock _statusBlock;
    private readonly TextBlock _messageBlock;
    private readonly TextBlock _modeBlock;
    private readonly TextBlock _runtimeBlock;
    private readonly Grid _boardGrid;
    private readonly StackPanel _historyPanel;
    private readonly StackPanel _promotionPanel;
    private readonly TextBlock _promotionPrompt;
    private readonly TextBox _saveBox;
    private readonly ComboBox _difficultyBox;
    private readonly ComboBox _humanColorBox;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _gameplay = _viewModel.Gameplay;

        Title = _viewModel.Title;
        Width = 1220;
        Height = 860;
        MinWidth = 980;
        MinHeight = 760;

        _modeBlock = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        };
        _statusBlock = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeight.SemiBold
        };
        _messageBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedTextBrush
        };
        _runtimeBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = MutedTextBrush,
            FontSize = 12
        };
        _promotionPrompt = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        _promotionPanel = new StackPanel
        {
            Spacing = 8,
            Orientation = Orientation.Horizontal,
            IsVisible = false
        };
        _historyPanel = new StackPanel { Spacing = 4 };
        _saveBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 180
        };
        _difficultyBox = new ComboBox
        {
            ItemsSource = _gameplay.AvailableDifficulties,
            SelectedItem = AiDifficulty.Medium,
            MinWidth = 180
        };
        _humanColorBox = new ComboBox
        {
            ItemsSource = _gameplay.AvailableHumanColors,
            SelectedItem = ChessColor.White,
            MinWidth = 180
        };
        _boardGrid = CreateBoardGrid();

        Content = BuildLayout();

        _gameplay.StateChanged += OnGameplayStateChanged;
        Closed += (_, _) => _gameplay.StateChanged -= OnGameplayStateChanged;
        RefreshUi();
    }

    private Control BuildLayout()
    {
        var root = new Grid
        {
            Margin = new Thickness(24),
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(new GridLength(3, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(2, GridUnitType.Star))
            }
        };

        var boardColumn = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = _viewModel.Title,
                    FontSize = 28,
                    FontWeight = FontWeight.Bold
                },
                new TextBlock
                {
                    Text = _viewModel.Subtitle,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = MutedTextBrush
                },
                _modeBlock,
                _statusBlock,
                _messageBlock,
                new Border
                {
                    Background = CreateBrush("#0F172A"),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(16),
                    Child = new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child = new Border
                        {
                            Width = 640,
                            Height = 640,
                            Child = _boardGrid
                        }
                    }
                },
                new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        _promotionPrompt,
                        _promotionPanel
                    }
                }
            }
        };
        Grid.SetColumn(boardColumn, 0);

        var sidePanel = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                BuildSetupSection(),
                BuildHistorySection(),
                BuildSaveSection()
            }
        };

        var sideScroll = new ScrollViewer
        {
            Content = sidePanel,
            Margin = new Thickness(24, 0, 0, 0)
        };
        Grid.SetColumn(sideScroll, 1);

        root.Children.Add(boardColumn);
        root.Children.Add(sideScroll);
        return root;
    }

    private Control BuildSetupSection()
    {
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var localButton = new Button { Content = "New local game" };
        localButton.Click += async (_, _) => await _gameplay.StartLocalGameAsync();

        var aiButton = new Button { Content = "New AI game" };
        aiButton.Click += async (_, _) =>
        {
            var humanColor = _humanColorBox.SelectedItem is ChessColor color ? color : ChessColor.White;
            var difficulty = _difficultyBox.SelectedItem is AiDifficulty selectedDifficulty ? selectedDifficulty : AiDifficulty.Medium;
            await _gameplay.StartComputerGameAsync(humanColor, difficulty);
        };

        buttonRow.Children.Add(localButton);
        buttonRow.Children.Add(aiButton);

        return new Border
        {
            Background = CreateBrush("#F8FAFC"),
            BorderBrush = CreateBrush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Game setup", FontSize = 20, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "AI difficulty" },
                    _difficultyBox,
                    new TextBlock { Text = "Human plays" },
                    _humanColorBox,
                    buttonRow,
                    _runtimeBlock
                }
            }
        };
    }

    private Control BuildHistorySection()
    {
        return new Border
        {
            Background = CreateBrush("#F8FAFC"),
            BorderBrush = CreateBrush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Move history", FontSize = 20, FontWeight = FontWeight.SemiBold },
                    new ScrollViewer
                    {
                        MaxHeight = 220,
                        Content = _historyPanel
                    }
                }
            }
        };
    }

    private Control BuildSaveSection()
    {
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var saveButton = new Button { Content = "Save snapshot" };
        saveButton.Click += (_, _) => _gameplay.SaveSnapshot();

        var loadButton = new Button { Content = "Load snapshot" };
        loadButton.Click += async (_, _) => await _gameplay.LoadSnapshotAsync(_saveBox.Text);

        buttonRow.Children.Add(saveButton);
        buttonRow.Children.Add(loadButton);

        return new Border
        {
            Background = CreateBrush("#F8FAFC"),
            BorderBrush = CreateBrush("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Save / load", FontSize = 20, FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "Use the snapshot box for manual save/load entry points.", TextWrapping = TextWrapping.Wrap, Foreground = MutedTextBrush },
                    buttonRow,
                    _saveBox
                }
            }
        };
    }

    private Grid CreateBoardGrid()
    {
        var grid = new Grid
        {
            RowSpacing = 0,
            ColumnSpacing = 0
        };

        for (var index = 0; index < 8; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        }

        return grid;
    }

    private void RefreshUi()
    {
        var state = _gameplay.State;
        _modeBlock.Text = state.ModeSummary;
        _statusBlock.Text = state.StatusText;
        _messageBlock.Text = state.MessageText;
        _runtimeBlock.Text = state.AiRuntimeSummary;
        _promotionPrompt.Text = state.PendingPromotionPrompt;
        _promotionPanel.IsVisible = state.HasPendingPromotion;

        if (!string.Equals(_saveBox.Text, state.SaveDraft, StringComparison.Ordinal))
        {
            _saveBox.Text = state.SaveDraft;
        }

        if (_difficultyBox.SelectedItem is not AiDifficulty selectedDifficulty || selectedDifficulty != state.SelectedDifficulty)
        {
            _difficultyBox.SelectedItem = state.SelectedDifficulty;
        }

        if (state.ComputerPlayerColor is ChessColor computerColor)
        {
            var humanColor = computerColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
            if (_humanColorBox.SelectedItem is not ChessColor selectedHumanColor || selectedHumanColor != humanColor)
            {
                _humanColorBox.SelectedItem = humanColor;
            }
        }

        RenderBoard(state);
        RenderHistory(state);
        RenderPromotionChoices(state);
    }

    private void RenderBoard(GameplayViewState state)
    {
        _boardGrid.Children.Clear();

        foreach (var square in state.BoardSquares)
        {
            var button = new Button
            {
                Content = square.PieceGlyph,
                FontSize = 38,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(0),
                Background = ResolveSquareBrush(square),
                Foreground = square.Piece?.Color == ChessColor.White ? WhitePieceBrush : BlackPieceBrush,
                IsEnabled = !state.IsBusy && !state.HasPendingPromotion && !state.IsComputerTurn
            };
            button.Click += async (_, _) => await _gameplay.SelectSquareAsync(square.Position);

            Grid.SetRow(button, square.DisplayRow);
            Grid.SetColumn(button, square.DisplayColumn);
            _boardGrid.Children.Add(button);
        }
    }

    private void RenderHistory(GameplayViewState state)
    {
        _historyPanel.Children.Clear();

        if (state.MoveHistory.Count == 0)
        {
            _historyPanel.Children.Add(new TextBlock
            {
                Text = "No moves yet.",
                Foreground = MutedTextBrush
            });
            return;
        }

        foreach (var entry in state.MoveHistory)
        {
            _historyPanel.Children.Add(new TextBlock { Text = entry });
        }
    }

    private void RenderPromotionChoices(GameplayViewState state)
    {
        _promotionPanel.Children.Clear();

        foreach (var pieceType in state.PendingPromotionChoices)
        {
            var choiceButton = new Button
            {
                Content = pieceType.ToString(),
                IsEnabled = !state.IsBusy
            };
            choiceButton.Click += async (_, _) => await _gameplay.ChoosePromotionAsync(pieceType);
            _promotionPanel.Children.Add(choiceButton);
        }
    }

    private void OnGameplayStateChanged(object? sender, EventArgs eventArgs)
    {
        Dispatcher.UIThread.Post(RefreshUi);
    }

    private static IBrush ResolveSquareBrush(ChessBoardSquareViewState square)
    {
        if (square.IsSelected)
        {
            return SelectedSquareBrush;
        }

        if (square.IsLegalDestination)
        {
            return LegalTargetBrush;
        }

        return square.IsLightSquare ? LightSquareBrush : DarkSquareBrush;
    }

    private static IBrush CreateBrush(string hex)
    {
        return new SolidColorBrush(Color.Parse(hex));
    }
}
