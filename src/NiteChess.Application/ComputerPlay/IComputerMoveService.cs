using NiteChess.Domain.Chess;

namespace NiteChess.Application.ComputerPlay;

public interface IComputerMoveService
{
    ValueTask<ChessMove> GetMoveAsync(
        GameSessions.LocalGameSession session,
        AiDifficulty difficulty,
        CancellationToken cancellationToken = default);
}