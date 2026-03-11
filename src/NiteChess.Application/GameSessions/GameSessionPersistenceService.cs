using System.Text.Json;
using System.Text.Json.Serialization;
using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public sealed class GameSessionPersistenceService : IGameSessionPersistenceService
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public string Save(LocalGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return JsonSerializer.Serialize(ToPersistedSession(session), SerializerOptions);
    }

    public LocalGameSession Load(string serializedSession)
    {
        if (string.IsNullOrWhiteSpace(serializedSession))
        {
            throw new ArgumentException("Serialized session payload must not be blank.", nameof(serializedSession));
        }

        var persistedSession = JsonSerializer.Deserialize<PersistedSession>(serializedSession, SerializerOptions)
            ?? throw new InvalidOperationException("Serialized session payload could not be deserialized.");

        if (persistedSession.Version != CurrentVersion)
        {
            throw new NotSupportedException($"Session payload version '{persistedSession.Version}' is not supported.");
        }

        return new LocalGameSession(
            persistedSession.SessionId,
            RestoreGame(persistedSession.Game),
            persistedSession.MoveHistory.Select(RestoreMoveRecord).ToArray(),
            RestorePendingPromotion(persistedSession.PendingPromotion));
    }

    private static PersistedSession ToPersistedSession(LocalGameSession session)
    {
        return new PersistedSession(
            CurrentVersion,
            session.SessionId,
            ToPersistedGame(session.Game),
            session.MoveHistory.Select(ToPersistedMoveRecord).ToArray(),
            ToPersistedPendingPromotion(session.PendingPromotion));
    }

    private static PersistedGame ToPersistedGame(ChessGame game)
    {
        return new PersistedGame(
            game.Board.GetOccupiedSquares()
                .Select(square => new PersistedSquare(square.Position.ToString(), square.Piece.Color, square.Piece.Type))
                .ToArray(),
            game.SideToMove,
            game.CastlingRights,
            game.EnPassantTarget?.ToString(),
            game.HalfmoveClock,
            game.FullmoveNumber);
    }

    private static ChessGame RestoreGame(PersistedGame game)
    {
        var board = ChessBoard.CreateEmpty();

        foreach (var square in game.Board)
        {
            board = board.WithPiece(ChessPosition.Parse(square.Position), new ChessPiece(square.Color, square.Type));
        }

        return new ChessGame(
            board,
            game.SideToMove,
            game.CastlingRights,
            ParsePosition(game.EnPassantTarget),
            game.HalfmoveClock,
            game.FullmoveNumber);
    }

    private static PersistedMoveRecord ToPersistedMoveRecord(GameSessionMoveRecord record)
    {
        return new PersistedMoveRecord(
            record.PlyNumber,
            record.TurnNumber,
            record.Player,
            record.Move.ToString(),
            record.Piece.Color,
            record.Piece.Type,
            record.CapturedPiece?.Color,
            record.CapturedPiece?.Type,
            record.IsEnPassant,
            record.IsCastling,
            record.ResultingStatus);
    }

    private static GameSessionMoveRecord RestoreMoveRecord(PersistedMoveRecord record)
    {
        ChessPiece? capturedPiece = null;

        if (record.CapturedPieceColor is ChessColor capturedColor && record.CapturedPieceType is PieceType capturedType)
        {
            capturedPiece = new ChessPiece(capturedColor, capturedType);
        }

        return new GameSessionMoveRecord(
            record.PlyNumber,
            record.TurnNumber,
            record.Player,
            ChessMove.Parse(record.Move),
            new ChessPiece(record.PieceColor, record.PieceType),
            capturedPiece,
            record.IsEnPassant,
            record.IsCastling,
            record.ResultingStatus);
    }

    private static PersistedPendingPromotion? ToPersistedPendingPromotion(PendingPromotionSelection? pendingPromotion)
    {
        return pendingPromotion is null
            ? null
            : new PersistedPendingPromotion(
                pendingPromotion.From.ToString(),
                pendingPromotion.To.ToString(),
                pendingPromotion.Player,
                pendingPromotion.AvailablePieceTypes.ToArray());
    }

    private static PendingPromotionSelection? RestorePendingPromotion(PersistedPendingPromotion? pendingPromotion)
    {
        return pendingPromotion is null
            ? null
            : new PendingPromotionSelection(
                ChessPosition.Parse(pendingPromotion.From),
                ChessPosition.Parse(pendingPromotion.To),
                pendingPromotion.Player,
                pendingPromotion.AvailablePieceTypes);
    }

    private static ChessPosition? ParsePosition(string? notation)
    {
        return notation is null ? null : ChessPosition.Parse(notation);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record PersistedSession(
        int Version,
        Guid SessionId,
        PersistedGame Game,
        PersistedMoveRecord[] MoveHistory,
        PersistedPendingPromotion? PendingPromotion);

    private sealed record PersistedGame(
        PersistedSquare[] Board,
        ChessColor SideToMove,
        CastlingRights CastlingRights,
        string? EnPassantTarget,
        int HalfmoveClock,
        int FullmoveNumber);

    private sealed record PersistedSquare(string Position, ChessColor Color, PieceType Type);

    private sealed record PersistedMoveRecord(
        int PlyNumber,
        int TurnNumber,
        ChessColor Player,
        string Move,
        ChessColor PieceColor,
        PieceType PieceType,
        ChessColor? CapturedPieceColor,
        PieceType? CapturedPieceType,
        bool IsEnPassant,
        bool IsCastling,
        ChessGameStatus ResultingStatus);

    private sealed record PersistedPendingPromotion(
        string From,
        string To,
        ChessColor Player,
        PieceType[] AvailablePieceTypes);
}