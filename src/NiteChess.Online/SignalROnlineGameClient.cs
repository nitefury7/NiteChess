using Microsoft.AspNetCore.SignalR.Client;
using NiteChess.Domain.Chess;
using NiteChess.Online.Contracts;

namespace NiteChess.Online;

public sealed class SignalROnlineGameClient : IOnlineGameClient, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private HubConnection? _connection;
    private Uri? _serverUri;
    private string? _roomCode;
    private string? _playerToken;

    public event Action<OnlineGameRoomState>? RoomStateChanged;

    public event Action<OnlineConnectionStatus>? ConnectionStatusChanged;

    public async ValueTask<OnlineRoomConnectionResult> CreateRoomAsync(Uri serverUri, string playerName, CancellationToken cancellationToken = default)
    {
        var request = new CreateRoomRequest(NormalizePlayerName(playerName));
        return await InvokeRoomConnectionAsync(serverUri, GameHubMethods.CreateRoom, request, cancellationToken);
    }

    public async ValueTask<OnlineRoomConnectionResult> JoinRoomAsync(Uri serverUri, string roomCode, string playerName, CancellationToken cancellationToken = default)
    {
        var request = new JoinRoomRequest(NormalizeRoomCode(roomCode), NormalizePlayerName(playerName));
        return await InvokeRoomConnectionAsync(serverUri, GameHubMethods.JoinRoom, request, cancellationToken);
    }

    public async ValueTask<OnlineRoomConnectionResult> ResumeRoomAsync(Uri serverUri, string roomCode, string playerToken, CancellationToken cancellationToken = default)
    {
        var request = new ResumeRoomRequest(NormalizeRoomCode(roomCode), NormalizePlayerToken(playerToken));
        return await InvokeRoomConnectionAsync(serverUri, GameHubMethods.ResumeRoom, request, cancellationToken);
    }

    public async ValueTask<OnlineGameActionResult> SubmitMoveAsync(string from, string to, CancellationToken cancellationToken = default)
    {
        var request = new OnlineMoveRequest(
            ChessPosition.Parse(from).ToString(),
            ChessPosition.Parse(to).ToString());

        return await InvokeActionAsync(GameHubMethods.SubmitMove, request, cancellationToken);
    }

    public async ValueTask<OnlineGameActionResult> CompletePromotionAsync(PieceType promotionPieceType, CancellationToken cancellationToken = default)
    {
        if (promotionPieceType is not (PieceType.Queen or PieceType.Rook or PieceType.Bishop or PieceType.Knight))
        {
            throw new ArgumentOutOfRangeException(nameof(promotionPieceType), promotionPieceType, "Only queen, rook, bishop, or knight promotions are supported.");
        }

        return await InvokeActionAsync(
            GameHubMethods.CompletePromotion,
            new OnlinePromotionRequest(promotionPieceType),
            cancellationToken);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _roomCode = null;
            _playerToken = null;
            await DisposeConnectionAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        RaiseConnectionStatus(new OnlineConnectionStatus("Not connected.", false, false));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
    }

    private async ValueTask<OnlineRoomConnectionResult> InvokeRoomConnectionAsync<TRequest>(
        Uri serverUri,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var connection = await EnsureConnectedAsync(serverUri, cancellationToken);
            var result = await connection.InvokeAsync<OnlineRoomConnectionResult>(methodName, request, cancellationToken);
            _serverUri = serverUri;
            _roomCode = result.RoomCode;
            _playerToken = result.PlayerToken;
            RaiseConnectionStatus(new OnlineConnectionStatus($"Connected to {serverUri}. Auto-reconnect is enabled.", true, false));
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<OnlineGameActionResult> InvokeActionAsync<TRequest>(string methodName, TRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var connection = _connection;
            if (connection is null || connection.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connect to an online room before sending online moves.");
            }

            var result = await connection.InvokeAsync<OnlineGameActionResult>(methodName, request, cancellationToken);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<HubConnection> EnsureConnectedAsync(Uri serverUri, CancellationToken cancellationToken)
    {
        if (_connection is not null && _serverUri is not null && _serverUri != serverUri)
        {
            _roomCode = null;
            _playerToken = null;
            await DisposeConnectionAsync(cancellationToken);
        }

        if (_connection is null)
        {
            _connection = BuildConnection(serverUri);
            _serverUri = serverUri;
        }

        if (_connection.State == HubConnectionState.Connected)
        {
            return _connection;
        }

        RaiseConnectionStatus(new OnlineConnectionStatus($"Connecting to {serverUri}…", false, false));
        await _connection.StartAsync(cancellationToken);
        return _connection;
    }

    private HubConnection BuildConnection(Uri serverUri)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUri(serverUri))
            .WithAutomaticReconnect()
            .Build();

        connection.On<OnlineGameRoomState>(nameof(IGameClient.RoomStateUpdated), roomState =>
        {
            RoomStateChanged?.Invoke(roomState);
        });

        connection.On<ServerHeartbeat>(nameof(IGameClient.ServerHeartbeat), _ => { });

        connection.Reconnecting += error =>
        {
            var detail = error is null ? "Reconnecting to multiplayer server…" : $"Reconnecting after transport drop: {error.Message}";
            RaiseConnectionStatus(new OnlineConnectionStatus(detail, false, true));
            return Task.CompletedTask;
        };

        connection.Reconnected += async _ =>
        {
            RaiseConnectionStatus(new OnlineConnectionStatus("Transport restored. Resuming online room…", true, false));
            await TryResumeAfterReconnectAsync(connection);
        };

        connection.Closed += error =>
        {
            var detail = error is null ? "Disconnected from multiplayer server." : $"Disconnected from multiplayer server: {error.Message}";
            RaiseConnectionStatus(new OnlineConnectionStatus(detail, false, false));
            return Task.CompletedTask;
        };

        return connection;
    }

    private async Task TryResumeAfterReconnectAsync(HubConnection connection)
    {
        if (_serverUri is null || string.IsNullOrWhiteSpace(_roomCode) || string.IsNullOrWhiteSpace(_playerToken))
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (!ReferenceEquals(_connection, connection) || connection.State != HubConnectionState.Connected)
            {
                return;
            }

            var result = await connection.InvokeAsync<OnlineRoomConnectionResult>(
                GameHubMethods.ResumeRoom,
                new ResumeRoomRequest(_roomCode, _playerToken),
                CancellationToken.None);

            RoomStateChanged?.Invoke(result.RoomState);
            RaiseConnectionStatus(new OnlineConnectionStatus($"Rejoined room {result.RoomCode} as {result.PlayerColor}.", true, false));
        }
        catch (Exception exception)
        {
            RaiseConnectionStatus(new OnlineConnectionStatus(
                $"Transport reconnected, but room resume failed: {exception.Message}",
                true,
                false));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask DisposeConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = _connection;
        _connection = null;
        _serverUri = null;

        if (connection is null)
        {
            return;
        }

        try
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private static Uri BuildHubUri(Uri serverUri)
    {
        var baseUri = serverUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? serverUri
            : new Uri(serverUri.AbsoluteUri + "/", UriKind.Absolute);

        return new Uri(baseUri, GameHubRoutes.GameHub.TrimStart('/'));
    }

    private static string NormalizePlayerName(string playerName)
    {
        var trimmed = playerName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Player name must not be blank.", nameof(playerName));
        }

        return trimmed.Length <= 32 ? trimmed : trimmed[..32];
    }

    private static string NormalizeRoomCode(string roomCode)
    {
        var trimmed = roomCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Room code must not be blank.", nameof(roomCode));
        }

        return trimmed;
    }

    private static string NormalizePlayerToken(string playerToken)
    {
        var trimmed = playerToken?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Reconnect token must not be blank.", nameof(playerToken));
        }

        return trimmed;
    }

    private void RaiseConnectionStatus(OnlineConnectionStatus status)
    {
        ConnectionStatusChanged?.Invoke(status);
    }
}