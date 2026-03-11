using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public sealed class GameSessionService : IGameSessionService
{
    public LocalGameSession CreateSession(ChessGame? initialGame = null, Guid? sessionId = null)
    {
        return new LocalGameSession(sessionId ?? Guid.NewGuid(), initialGame ?? ChessGame.CreateInitial());
    }

    public SessionMoveResult SubmitMove(LocalGameSession session, ChessPosition from, ChessPosition to)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.PendingPromotion is not null)
        {
            return Reject(
                GameSessionMoveOutcome.PendingPromotionSelectionRequired,
                session,
                "A promotion choice is still pending for the previous move.");
        }

        if (session.IsComplete)
        {
            return Reject(GameSessionMoveOutcome.GameAlreadyFinished, session, "The game session has already ended.");
        }

        var candidateMoves = session.Game.GetLegalMoves(from)
            .Where(move => move.To == to)
            .ToArray();

        if (candidateMoves.Length == 0)
        {
            return Reject(GameSessionMoveOutcome.IllegalMove, session, $"Move '{from}{to}' is not legal in the current session.");
        }

        var promotionMoves = candidateMoves
            .Where(move => move.PromotionPieceType is not null)
            .ToArray();

        if (promotionMoves.Length == candidateMoves.Length)
        {
            var pendingPromotion = new PendingPromotionSelection(
                from,
                to,
                session.Game.SideToMove,
                promotionMoves.Select(move => move.PromotionPieceType!.Value).ToArray());

            return new SessionMoveResult(
                GameSessionMoveOutcome.PromotionSelectionRequired,
                new LocalGameSession(session.SessionId, session.Game, session.MoveHistory, pendingPromotion));
        }

        return ApplyMove(session, candidateMoves[0]);
    }

    public SessionMoveResult SubmitMove(LocalGameSession session, ChessMove move)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (move.PromotionPieceType is null)
        {
            return SubmitMove(session, move.From, move.To);
        }

        if (session.PendingPromotion is not null)
        {
            return Reject(
                GameSessionMoveOutcome.PendingPromotionSelectionRequired,
                session,
                "A promotion choice is still pending for the previous move.");
        }

        if (session.IsComplete)
        {
            return Reject(GameSessionMoveOutcome.GameAlreadyFinished, session, "The game session has already ended.");
        }

        if (!session.Game.IsLegalMove(move))
        {
            return Reject(GameSessionMoveOutcome.IllegalMove, session, $"Move '{move}' is not legal in the current session.");
        }

        return ApplyMove(session, move);
    }

    public SessionMoveResult CompletePromotion(LocalGameSession session, PieceType promotionPieceType)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.PendingPromotion is null)
        {
            return Reject(GameSessionMoveOutcome.IllegalMove, session, "No promotion choice is currently pending.");
        }

        if (!session.PendingPromotion.AvailablePieceTypes.Contains(promotionPieceType))
        {
            return Reject(GameSessionMoveOutcome.IllegalMove, session, $"'{promotionPieceType}' is not an allowed promotion choice.");
        }

        var promotionMove = new ChessMove(
            session.PendingPromotion.From,
            session.PendingPromotion.To,
            promotionPieceType);

        if (!session.Game.IsLegalMove(promotionMove))
        {
            return Reject(GameSessionMoveOutcome.IllegalMove, session, $"Move '{promotionMove}' is no longer legal in the current session.");
        }

        return ApplyMove(session, promotionMove);
    }

    private static SessionMoveResult ApplyMove(LocalGameSession session, ChessMove move)
    {
        var currentGame = session.Game;
        var movingPiece = currentGame.Board[move.From]
            ?? throw new InvalidOperationException($"No piece exists at '{move.From}'.");
        var capturedPiece = ResolveCapturedPiece(currentGame, move, movingPiece);
        var nextGame = currentGame.ApplyMove(move);
        var historyEntry = new GameSessionMoveRecord(
            session.MoveHistory.Count + 1,
            GetTurnNumber(session.MoveHistory.Count + 1),
            movingPiece.Color,
            move,
            movingPiece,
            capturedPiece,
            IsEnPassantCapture(currentGame, move, movingPiece),
            IsCastlingMove(movingPiece, move),
            nextGame.GetStatus());
        var nextHistory = session.MoveHistory.Concat(new[] { historyEntry }).ToArray();
        var nextSession = new LocalGameSession(session.SessionId, nextGame, nextHistory);

        return new SessionMoveResult(GameSessionMoveOutcome.Applied, nextSession, historyEntry);
    }

    private static ChessPiece? ResolveCapturedPiece(ChessGame game, ChessMove move, ChessPiece movingPiece)
    {
        if (game.Board[move.To] is ChessPiece capturedOnTarget)
        {
            return capturedOnTarget;
        }

        if (!IsEnPassantCapture(game, move, movingPiece))
        {
            return null;
        }

        return game.Board[new ChessPosition(move.To.File, move.From.Rank)];
    }

    private static bool IsEnPassantCapture(ChessGame game, ChessMove move, ChessPiece movingPiece)
    {
        return movingPiece.Type == PieceType.Pawn &&
               game.EnPassantTarget is ChessPosition enPassantTarget &&
               enPassantTarget == move.To &&
               game.Board[move.To] is null &&
               move.From.File != move.To.File;
    }

    private static bool IsCastlingMove(ChessPiece movingPiece, ChessMove move)
    {
        return movingPiece.Type == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2;
    }

    private static int GetTurnNumber(int plyNumber)
    {
        return ((plyNumber - 1) / 2) + 1;
    }

    private static SessionMoveResult Reject(GameSessionMoveOutcome outcome, LocalGameSession session, string reason)
    {
        return new SessionMoveResult(outcome, session, rejectionReason: reason);
    }
}