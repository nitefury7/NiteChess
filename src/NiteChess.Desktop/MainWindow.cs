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
    private static readonly IBrush LightSquareBrush = CreateBrush("#E7EFD0");
    private static readonly IBrush DarkSquareBrush = CreateBrush("#769656");
    private static readonly IBrush BoardFrameBrush = CreateBrush("#7C5A36");
    private static readonly IBrush BoardFrameAccentBrush = CreateBrush("#D9C38A");
    private static readonly IBrush BoardOutlineBrush = CreateBrush("#4B341C");
    private static readonly IBrush SelectedSquareBrush = CreateBrush("#FFF2A8");
    private static readonly IBrush SelectedSquareFillBrush = CreateBrush("#99FFF7C7");
    private static readonly IBrush LegalTargetBrush = CreateBrush("#1F2937");
    private static readonly IBrush LegalTargetFillBrush = CreateBrush("#551F2937");
    private static readonly IBrush WhitePieceBrush = CreateBrush("#FFFDF5");
    private static readonly IBrush WhitePieceShadowBrush = CreateBrush("#CC111111");
    private static readonly IBrush BlackPieceBrush = CreateBrush("#111827");
    private static readonly IBrush BlackPieceShadowBrush = CreateBrush("#66FFF8DC");
    private static readonly IBrush MutedTextBrush = CreateBrush("#475569");
    private static readonly IBrush SidebarCardBrush = CreateBrush("#0F172A");
    private static readonly IBrush SidebarCardBorderBrush = CreateBrush("#334155");
    private static readonly IBrush SidebarTextBrush = CreateBrush("#F8FAFC");
    private static readonly IBrush SidebarMutedTextBrush = CreateBrush("#CBD5E1");
    private static readonly IBrush SidebarInputBrush = CreateBrush("#111827");
    private static readonly IBrush SidebarInputBorderBrush = CreateBrush("#475569");
    private static readonly IBrush SidebarPrimaryButtonBrush = CreateBrush("#2563EB");
    private static readonly IBrush SidebarPrimaryButtonBorderBrush = CreateBrush("#3B82F6");

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
    private readonly TextBox _onlineServerUrlBox;
    private readonly TextBox _onlinePlayerNameBox;
    private readonly TextBox _onlineRoomCodeBox;
    private readonly TextBox _onlinePlayerTokenBox;
    private readonly TextBlock _onlineConnectionBlock;
    private readonly TextBlock _onlinePlayersBlock;
    private readonly TextBlock _onlineRoleBlock;
    private Border _boardFrame = null!;
    private Border _boardSurface = null!;

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
        _historyPanel = new StackPanel { Spacing = 6 };
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
        _onlineServerUrlBox = new TextBox { Watermark = "https://localhost:5001" };
        _onlineServerUrlBox.TextChanged += (_, _) => _gameplay.UpdateOnlineServerUrl(_onlineServerUrlBox.Text);
        _onlinePlayerNameBox = new TextBox { Watermark = "Player name" };
        _onlinePlayerNameBox.TextChanged += (_, _) => _gameplay.UpdateOnlinePlayerName(_onlinePlayerNameBox.Text);
        _onlineRoomCodeBox = new TextBox { Watermark = "ABC123" };
        _onlineRoomCodeBox.TextChanged += (_, _) => _gameplay.UpdateOnlineRoomCode(_onlineRoomCodeBox.Text);
        _onlinePlayerTokenBox = new TextBox { Watermark = "Reconnect token" };
        _onlinePlayerTokenBox.TextChanged += (_, _) => _gameplay.UpdateOnlinePlayerToken(_onlinePlayerTokenBox.Text);
        _onlineConnectionBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = SidebarMutedTextBrush };
        _onlinePlayersBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = SidebarMutedTextBrush };
        _onlineRoleBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = SidebarMutedTextBrush };
        StyleSidebarComboBox(_difficultyBox);
        StyleSidebarComboBox(_humanColorBox);
        StyleSidebarTextBox(_saveBox);
        StyleSidebarTextBox(_onlineServerUrlBox);
        StyleSidebarTextBox(_onlinePlayerNameBox);
        StyleSidebarTextBox(_onlineRoomCodeBox);
        StyleSidebarTextBox(_onlinePlayerTokenBox);
        _runtimeBlock.Foreground = SidebarMutedTextBrush;
        _boardGrid = CreateBoardGrid();

        Content = BuildLayout();
        SizeChanged += (_, _) => AdjustBoardSurfaceSize();
        _boardFrame.SizeChanged += (_, _) => AdjustBoardSurfaceSize();

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

        var headerPanel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = _viewModel.Title,
                    FontSize = 28,
                    FontWeight = FontWeight.Bold
                },
                _modeBlock,
                _statusBlock
            }
        };

        var boardColumn = new Grid
        {
            RowSpacing = 14,
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(new GridLength(1, GridUnitType.Star)),
                new RowDefinition(GridLength.Auto)
            }
        };

        Grid.SetRow(headerPanel, 0);
        boardColumn.Children.Add(headerPanel);

        _boardSurface = new Border
        {
            Width = 640,
            Height = 640,
            Background = BoardOutlineBrush,
            BorderBrush = BoardFrameAccentBrush,
            BorderThickness = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = _boardGrid
        };

        _boardFrame = new Border
        {
            Background = BoardFrameBrush,
            BorderBrush = BoardFrameAccentBrush,
            BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(18),
            Child = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Children = { _boardSurface }
            }
        };
        Grid.SetRow(_boardFrame, 1);
        boardColumn.Children.Add(_boardFrame);

        var promotionSection = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                _promotionPrompt,
                _promotionPanel
            }
        };
        Grid.SetRow(promotionSection, 2);
        boardColumn.Children.Add(promotionSection);
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
            Spacing = 8
        };

        var localButton = new Button { Content = "New local game" };
        StyleSidebarButton(localButton);
        localButton.Click += async (_, _) => await _gameplay.StartLocalGameAsync();

        var aiButton = new Button { Content = "New AI game" };
        StyleSidebarButton(aiButton);
        aiButton.Click += async (_, _) =>
        {
            var humanColor = _humanColorBox.SelectedItem is ChessColor color ? color : ChessColor.White;
            var difficulty = _difficultyBox.SelectedItem is AiDifficulty selectedDifficulty ? selectedDifficulty : AiDifficulty.Medium;
            await _gameplay.StartComputerGameAsync(humanColor, difficulty);
        };

        var createRoomButton = new Button { Content = "Create room" };
        StyleSidebarButton(createRoomButton);
        createRoomButton.Click += async (_, _) => await _gameplay.CreateOnlineGameAsync();

        var joinRoomButton = new Button { Content = "Join room" };
        StyleSidebarButton(joinRoomButton);
        joinRoomButton.Click += async (_, _) => await _gameplay.JoinOnlineGameAsync();

        var resumeRoomButton = new Button { Content = "Resume room" };
        StyleSidebarButton(resumeRoomButton);
        resumeRoomButton.Click += async (_, _) => await _gameplay.ResumeOnlineGameAsync();

        buttonRow.Children.Add(localButton);
        buttonRow.Children.Add(aiButton);

        var onlineButtonRow = new StackPanel
        {
            Spacing = 8,
            Children = { createRoomButton, joinRoomButton, resumeRoomButton }
        };

        return CreateSidebarCard(
            new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    CreateSidebarHeading("Game setup"),
                    CreateSidebarLabel("AI difficulty"),
                    _difficultyBox,
                    CreateSidebarLabel("Human plays"),
                    _humanColorBox,
                    buttonRow,
                    _runtimeBlock,
                    CreateSidebarDivider(),
                    CreateSidebarHeading("Online multiplayer"),
                    CreateSidebarLabel("Backend URL"),
                    _onlineServerUrlBox,
                    CreateSidebarLabel("Player name"),
                    _onlinePlayerNameBox,
                    CreateSidebarLabel("Room code"),
                    _onlineRoomCodeBox,
                    CreateSidebarLabel("Reconnect token"),
                    _onlinePlayerTokenBox,
                    onlineButtonRow,
                    _onlineConnectionBlock,
                    _onlinePlayersBlock,
                    _onlineRoleBlock
                }
            });
    }

    private Control BuildHistorySection()
    {
        return CreateSidebarCard(
            new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    CreateSidebarHeading("Move history"),
                    new ScrollViewer
                    {
                        MaxHeight = 220,
                        Content = _historyPanel
                    }
                }
            });
    }

    private Control BuildSaveSection()
    {
        var buttonRow = new StackPanel
        {
            Spacing = 8
        };

        var saveButton = new Button { Content = "Save snapshot" };
        StyleSidebarButton(saveButton);
        saveButton.Click += (_, _) => _gameplay.SaveSnapshot();

        var loadButton = new Button { Content = "Load snapshot" };
        StyleSidebarButton(loadButton);
        loadButton.Click += async (_, _) => await _gameplay.LoadSnapshotAsync(_saveBox.Text);

        buttonRow.Children.Add(saveButton);
        buttonRow.Children.Add(loadButton);

        return CreateSidebarCard(
            new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    CreateSidebarHeading("Save / load"),
                    new TextBlock
                    {
                        Text = "Use the snapshot box for manual save/load entry points.",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = SidebarMutedTextBrush
                    },
                    buttonRow,
                    _saveBox
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
        _onlineConnectionBlock.Text = $"Connection: {state.OnlinePlay.ConnectionSummary}";
        _onlinePlayersBlock.Text = $"Players: {state.OnlinePlay.SeatSummary}";
        _onlineRoleBlock.Text = state.OnlinePlay.PlayerColor is ChessColor playerColor
            ? $"You are: {FormatColor(playerColor)}"
            : string.Empty;

        if (!string.Equals(_saveBox.Text, state.SaveDraft, StringComparison.Ordinal))
        {
            _saveBox.Text = state.SaveDraft;
        }

        if (!string.Equals(_onlineServerUrlBox.Text, state.OnlinePlay.ServerUrl, StringComparison.Ordinal))
        {
            _onlineServerUrlBox.Text = state.OnlinePlay.ServerUrl;
        }

        if (!string.Equals(_onlinePlayerNameBox.Text, state.OnlinePlay.PlayerName, StringComparison.Ordinal))
        {
            _onlinePlayerNameBox.Text = state.OnlinePlay.PlayerName;
        }

        if (!string.Equals(_onlineRoomCodeBox.Text, state.OnlinePlay.RoomCode, StringComparison.Ordinal))
        {
            _onlineRoomCodeBox.Text = state.OnlinePlay.RoomCode;
        }

        if (!string.Equals(_onlinePlayerTokenBox.Text, state.OnlinePlay.PlayerToken, StringComparison.Ordinal))
        {
            _onlinePlayerTokenBox.Text = state.OnlinePlay.PlayerToken;
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
        AdjustBoardSurfaceSize();
    }

    private void RenderBoard(GameplayViewState state)
    {
        _boardGrid.Children.Clear();

        foreach (var square in state.BoardSquares)
        {
            var squareContent = new Grid { IsHitTestVisible = false };

            if (square.IsSelected)
            {
                squareContent.Children.Add(new Border
                {
                    Background = SelectedSquareFillBrush,
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    IsHitTestVisible = false
                });
            }

            if (square.IsLegalDestination)
            {
                squareContent.Children.Add(square.Piece is null
                    ? new Border
                    {
                        Width = 18,
                        Height = 18,
                        Background = LegalTargetFillBrush,
                        CornerRadius = new CornerRadius(9),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    }
                    : new Border
                    {
                        BorderBrush = LegalTargetBrush,
                        BorderThickness = new Thickness(4),
                        Margin = new Thickness(8),
                        CornerRadius = new CornerRadius(8),
                        IsHitTestVisible = false
                    });
            }

            if (square.IsSelected)
            {
                squareContent.Children.Add(new Border
                {
                    BorderBrush = SelectedSquareBrush,
                    BorderThickness = new Thickness(4),
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(8),
                    IsHitTestVisible = false
                });
            }

            if (square.Piece is ChessPiece piece)
            {
                if (piece.Color == ChessColor.White)
                {
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), -1.5, 0);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 1.5, 0);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 0, -1.5);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 0, 1.5);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), -1.2, -1.2);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 1.2, -1.2);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), -1.2, 1.2);
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 1.2, 1.2);
                }
                else
                {
                    AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceShadowBrush(piece.Color), 1.4, 1.4);
                }

                AddPieceGlyphLayer(squareContent, square.PieceGlyph, ResolvePieceBrush(piece.Color));
            }

            var squareButton = new Button
            {
                Background = ResolveSquareBrush(square),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = squareContent,
                IsHitTestVisible = state.CanInteractWithBoard
            };
            squareButton.Click += async (_, _) =>
            {
                if (!state.CanInteractWithBoard)
                {
                    return;
                }

                await _gameplay.SelectSquareAsync(square.Position);
            };

            Grid.SetRow(squareButton, square.DisplayRow);
            Grid.SetColumn(squareButton, square.DisplayColumn);
            _boardGrid.Children.Add(squareButton);
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
                Foreground = SidebarMutedTextBrush
            });
            return;
        }

        foreach (var entry in state.MoveHistory)
        {
            _historyPanel.Children.Add(new TextBlock
            {
                Text = entry,
                Foreground = SidebarTextBrush,
                TextWrapping = TextWrapping.Wrap
            });
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
                IsEnabled = state.CanChoosePromotion
            };
            choiceButton.Click += async (_, _) => await _gameplay.ChoosePromotionAsync(pieceType);
            _promotionPanel.Children.Add(choiceButton);
        }
    }

    private void OnGameplayStateChanged(object? sender, EventArgs eventArgs)
    {
        Dispatcher.UIThread.Post(RefreshUi);
    }

    private void AdjustBoardSurfaceSize()
    {
        var availableWidth = _boardFrame.Bounds.Width - _boardFrame.Padding.Left - _boardFrame.Padding.Right - _boardFrame.BorderThickness.Left - _boardFrame.BorderThickness.Right;
        var availableHeight = _boardFrame.Bounds.Height - _boardFrame.Padding.Top - _boardFrame.Padding.Bottom - _boardFrame.BorderThickness.Top - _boardFrame.BorderThickness.Bottom;
        var targetSize = Math.Max(0, Math.Min(availableWidth, availableHeight));

        if (targetSize <= 0)
        {
            return;
        }

        if (Math.Abs(_boardSurface.Width - targetSize) > 0.5)
        {
            _boardSurface.Width = targetSize;
        }

        if (Math.Abs(_boardSurface.Height - targetSize) > 0.5)
        {
            _boardSurface.Height = targetSize;
        }
    }

    private static IBrush ResolveSquareBrush(ChessBoardSquareViewState square)
    {
        return square.IsLightSquare ? LightSquareBrush : DarkSquareBrush;
    }

    private static IBrush ResolvePieceBrush(ChessColor pieceColor)
    {
        return pieceColor switch
        {
            ChessColor.White => WhitePieceBrush,
            ChessColor.Black => BlackPieceBrush,
            _ => Brushes.Transparent
        };
    }

    private static IBrush ResolvePieceShadowBrush(ChessColor pieceColor)
    {
        return pieceColor switch
        {
            ChessColor.White => WhitePieceShadowBrush,
            ChessColor.Black => BlackPieceShadowBrush,
            _ => Brushes.Transparent
        };
    }

    private static void AddPieceGlyphLayer(Panel container, string glyph, IBrush brush, double offsetX = 0, double offsetY = 0)
    {
        var textBlock = new TextBlock
        {
            Text = glyph,
            FontSize = 46,
            FontWeight = FontWeight.Bold,
            Foreground = brush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false
        };

        if (Math.Abs(offsetX) > double.Epsilon || Math.Abs(offsetY) > double.Epsilon)
        {
            textBlock.RenderTransform = new TranslateTransform(offsetX, offsetY);
        }

        container.Children.Add(textBlock);
    }

    private static Border CreateSidebarCard(Control content)
    {
        return new Border
        {
            Background = SidebarCardBrush,
            BorderBrush = SidebarCardBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = content
        };
    }

    private static Border CreateSidebarDivider()
    {
        return new Border
        {
            Height = 1,
            Background = SidebarCardBorderBrush,
            Margin = new Thickness(0, 4)
        };
    }

    private static TextBlock CreateSidebarHeading(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            Foreground = SidebarTextBrush
        };
    }

    private static TextBlock CreateSidebarLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            Foreground = SidebarMutedTextBrush
        };
    }

    private static void StyleSidebarTextBox(TextBox textBox)
    {
        textBox.Background = SidebarInputBrush;
        textBox.Foreground = SidebarTextBrush;
        textBox.BorderBrush = SidebarInputBorderBrush;
        textBox.BorderThickness = new Thickness(1);
        textBox.Padding = new Thickness(12, 10);
        textBox.MinHeight = Math.Max(textBox.MinHeight, 40);
    }

    private static void StyleSidebarComboBox(ComboBox comboBox)
    {
        comboBox.Background = SidebarInputBrush;
        comboBox.Foreground = SidebarTextBrush;
        comboBox.BorderBrush = SidebarInputBorderBrush;
        comboBox.BorderThickness = new Thickness(1);
        comboBox.MinHeight = Math.Max(comboBox.MinHeight, 40);
    }

    private static void StyleSidebarButton(Button button)
    {
        button.Background = SidebarPrimaryButtonBrush;
        button.Foreground = SidebarTextBrush;
        button.BorderBrush = SidebarPrimaryButtonBorderBrush;
        button.BorderThickness = new Thickness(1);
        button.Padding = new Thickness(12, 10);
        button.MinHeight = 40;
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private static IBrush CreateBrush(string hex)
    {
        return new SolidColorBrush(Color.Parse(hex));
    }

    private static string FormatColor(ChessColor color)
    {
        return color == ChessColor.White ? "White" : "Black";
    }
}
