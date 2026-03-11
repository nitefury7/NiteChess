using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public interface IGameSessionService
{
    LocalGameSession CreateSession(ChessGame? initialGame = null, Guid? sessionId = null);

    SessionMoveResult SubmitMove(LocalGameSession session, ChessPosition from, ChessPosition to);

    SessionMoveResult SubmitMove(LocalGameSession session, ChessMove move);

    SessionMoveResult CompletePromotion(LocalGameSession session, PieceType promotionPieceType);
}