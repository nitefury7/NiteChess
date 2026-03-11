using Microsoft.Maui.ApplicationModel;
using NiteChess.Application.ComputerPlay;
using NiteChess.Application.Configuration;
using NiteChess.Application.Gameplay;
using NiteChess.Domain.Chess;

namespace NiteChess.Mobile;

public sealed class MainPage : ContentPage
{
    private readonly GameplayController _gameplay;
    private readonly Label _modeLabel;
    private readonly Label _statusLabel;
    private readonly Label _messageLabel;
    private readonly Label _runtimeLabel;
    private readonly Grid _boardGrid;
    private readonly VerticalStackLayout _historyStack;
    private readonly HorizontalStackLayout _promotionChoices;
    private readonly Label _promotionPrompt;
    private readonly Editor _saveEditor;
    private readonly Picker _difficultyPicker;
    private readonly Picker _humanColorPicker;
    private readonly Frame _boardFrame;

    public MainPage(NiteChessBootstrapManifest manifest, GameplayController gameplay)
    {
        _gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
        Title = "NiteChess Mobile";

        _modeLabel = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold
        };
        _statusLabel = new Label
        {
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        };
        _messageLabel = CreateSecondaryLabel();
        _runtimeLabel = CreateSecondaryLabel();
        _promotionPrompt = new Label
        {
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        };
        _promotionChoices = new HorizontalStackLayout
        {
            Spacing = 8,
            IsVisible = false
        };
        _historyStack = new VerticalStackLayout { Spacing = 4 };
        _saveEditor = new Editor
        {
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 180
        };
        _difficultyPicker = new Picker
        {
            Title = "AI difficulty",
            ItemsSource = _gameplay.AvailableDifficulties.ToList(),
            SelectedItem = AiDifficulty.Medium
        };
        _humanColorPicker = new Picker
        {
            Title = "Human plays",
            ItemsSource = _gameplay.AvailableHumanColors.ToList(),
            SelectedItem = ChessColor.White
        };
        _boardGrid = CreateBoardGrid();
        _boardFrame = new Frame
        {
            Padding = 0,
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb("#0F172A"),
            BorderColor = Color.FromArgb("#1E293B"),
            HasShadow = false,
            HorizontalOptions = LayoutOptions.Center,
            Content = _boardGrid
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "NiteChess Mobile",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = $"{manifest.Platform.Surface} board play, history, save/load, and offline AI.",
                        LineBreakMode = LineBreakMode.WordWrap,
                        TextColor = Color.FromArgb("#475569")
                    },
                    _modeLabel,
                    _statusLabel,
                    _messageLabel,
                    _boardFrame,
                    new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            _promotionPrompt,
                            _promotionChoices
                        }
                    },
                    CreateSetupCard(),
                    CreateHistoryCard(),
                    CreateSaveCard(),
                    _runtimeLabel
                }
            }
        };

        SizeChanged += (_, _) => AdjustBoardSize();
        _gameplay.StateChanged += OnGameplayStateChanged;
        RefreshUi();
    }

    private View CreateSetupCard()
    {
        var localButton = new Button { Text = "New local game" };
        localButton.Clicked += async (_, _) => await _gameplay.StartLocalGameAsync();

        var aiButton = new Button { Text = "New AI game" };
        aiButton.Clicked += async (_, _) =>
        {
            var humanColor = _humanColorPicker.SelectedItem is ChessColor color ? color : ChessColor.White;
            var difficulty = _difficultyPicker.SelectedItem is AiDifficulty selectedDifficulty ? selectedDifficulty : AiDifficulty.Medium;
            await _gameplay.StartComputerGameAsync(humanColor, difficulty);
        };

        return CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                CreateSectionHeader("Game setup"),
                _difficultyPicker,
                _humanColorPicker,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { localButton, aiButton }
                }
            }
        });
    }

    private View CreateHistoryCard()
    {
        return CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                CreateSectionHeader("Move history"),
                new ScrollView
                {
                    HeightRequest = 180,
                    Content = _historyStack
                }
            }
        });
    }

    private View CreateSaveCard()
    {
        var saveButton = new Button { Text = "Save snapshot" };
        saveButton.Clicked += (_, _) => _gameplay.SaveSnapshot();

        var loadButton = new Button { Text = "Load snapshot" };
        loadButton.Clicked += async (_, _) => await _gameplay.LoadSnapshotAsync(_saveEditor.Text);

        return CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                CreateSectionHeader("Save / load"),
                CreateSecondaryLabel("Use the snapshot box for manual save/load entry points."),
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { saveButton, loadButton }
                },
                _saveEditor
            }
        });
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
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        return grid;
    }

    private void RefreshUi()
    {
        var state = _gameplay.State;
        _modeLabel.Text = state.ModeSummary;
        _statusLabel.Text = state.StatusText;
        _messageLabel.Text = state.MessageText;
        _runtimeLabel.Text = state.AiRuntimeSummary;
        _promotionPrompt.Text = state.PendingPromotionPrompt;
        _promotionChoices.IsVisible = state.HasPendingPromotion;

        if (!string.Equals(_saveEditor.Text, state.SaveDraft, StringComparison.Ordinal))
        {
            _saveEditor.Text = state.SaveDraft;
        }

        if (_difficultyPicker.SelectedItem is not AiDifficulty selectedDifficulty || selectedDifficulty != state.SelectedDifficulty)
        {
            _difficultyPicker.SelectedItem = state.SelectedDifficulty;
        }

        if (state.ComputerPlayerColor is ChessColor computerColor)
        {
            var humanColor = computerColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
            if (_humanColorPicker.SelectedItem is not ChessColor selectedHumanColor || selectedHumanColor != humanColor)
            {
                _humanColorPicker.SelectedItem = humanColor;
            }
        }

        RenderBoard(state);
        RenderHistory(state);
        RenderPromotionChoices(state);
        AdjustBoardSize();
    }

    private void RenderBoard(GameplayViewState state)
    {
        _boardGrid.Children.Clear();

        foreach (var square in state.BoardSquares)
        {
            var button = new Button
            {
                Text = square.PieceGlyph,
                FontSize = 34,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 0,
                Padding = 0,
                BackgroundColor = ResolveSquareColor(square),
                TextColor = square.Piece?.Color == ChessColor.White ? Color.FromArgb("#F9FAFB") : Color.FromArgb("#111827"),
                IsEnabled = !state.IsBusy && !state.HasPendingPromotion && !state.IsComputerTurn
            };
            button.Clicked += async (_, _) => await _gameplay.SelectSquareAsync(square.Position);

            Grid.SetRow(button, square.DisplayRow);
            Grid.SetColumn(button, square.DisplayColumn);
            _boardGrid.Children.Add(button);
        }
    }

    private void RenderHistory(GameplayViewState state)
    {
        _historyStack.Children.Clear();

        if (state.MoveHistory.Count == 0)
        {
            _historyStack.Children.Add(CreateSecondaryLabel("No moves yet."));
            return;
        }

        foreach (var entry in state.MoveHistory)
        {
            _historyStack.Children.Add(new Label { Text = entry });
        }
    }

    private void RenderPromotionChoices(GameplayViewState state)
    {
        _promotionChoices.Children.Clear();

        foreach (var pieceType in state.PendingPromotionChoices)
        {
            var button = new Button
            {
                Text = pieceType.ToString(),
                IsEnabled = !state.IsBusy
            };
            button.Clicked += async (_, _) => await _gameplay.ChoosePromotionAsync(pieceType);
            _promotionChoices.Children.Add(button);
        }
    }

    private void AdjustBoardSize()
    {
        var availableWidth = Width > 0 ? Width - 48 : 320;
        var targetSize = Math.Min(Math.Max(availableWidth, 280), 520);
        _boardFrame.WidthRequest = targetSize;
        _boardFrame.HeightRequest = targetSize;
    }

    private void OnGameplayStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshUi);
    }

    private static Color ResolveSquareColor(ChessBoardSquareViewState square)
    {
        if (square.IsSelected)
        {
            return Color.FromArgb("#F6F669");
        }

        if (square.IsLegalDestination)
        {
            return Color.FromArgb("#BACA44");
        }

        return square.IsLightSquare ? Color.FromArgb("#EEEED2") : Color.FromArgb("#769656");
    }

    private static Label CreateSectionHeader(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold
        };
    }

    private static Label CreateSecondaryLabel(string text = "")
    {
        return new Label
        {
            Text = text,
            TextColor = Color.FromArgb("#475569"),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static View CreateCard(View content)
    {
        return new Frame
        {
            Padding = 16,
            CornerRadius = 12,
            HasShadow = false,
            BorderColor = Color.FromArgb("#CBD5E1"),
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Content = content
        };
    }
}
