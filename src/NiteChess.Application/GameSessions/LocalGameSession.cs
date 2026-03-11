using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public sealed class LocalGameSession
{
    public LocalGameSession(
        Guid sessionId,
        ChessGame game,
        IReadOnlyList<GameSessionMoveRecord>? moveHistory = null,
        PendingPromotionSelection? pendingPromotion = null)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session ID must not be empty.", nameof(sessionId));
        }

        SessionId = sessionId;
        Game = game ?? throw new ArgumentNullException(nameof(game));
        MoveHistory = moveHistory?.ToArray() ?? Array.Empty<GameSessionMoveRecord>();
        PendingPromotion = pendingPromotion;
    }

    public Guid SessionId { get; }

    public ChessGame Game { get; }

    public IReadOnlyList<GameSessionMoveRecord> MoveHistory { get; }

    public PendingPromotionSelection? PendingPromotion { get; }

    public ChessGameStatus Status => Game.GetStatus();

    public bool IsComplete => PendingPromotion is null && Status is ChessGameStatus.Checkmate or ChessGameStatus.Stalemate;
}