namespace NiteChess.Application.GameSessions;

public sealed class SessionMoveResult
{
    public SessionMoveResult(
        GameSessionMoveOutcome outcome,
        LocalGameSession session,
        GameSessionMoveRecord? appliedMove = null,
        string? rejectionReason = null)
    {
        Outcome = outcome;
        Session = session ?? throw new ArgumentNullException(nameof(session));
        AppliedMove = appliedMove;
        RejectionReason = rejectionReason;
    }

    public GameSessionMoveOutcome Outcome { get; }

    public LocalGameSession Session { get; }

    public GameSessionMoveRecord? AppliedMove { get; }

    public string? RejectionReason { get; }
}