using NiteChess.Application.GameSessions;
using NiteChess.Domain.Chess;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Application.ComputerPlay;

public sealed class StockfishComputerMoveService : IComputerMoveService
{
    private readonly IStockfishEngineClient _engineClient;

    public StockfishComputerMoveService(IStockfishEngineClient engineClient)
    {
        _engineClient = engineClient ?? throw new ArgumentNullException(nameof(engineClient));
    }

    public async ValueTask<ChessMove> GetMoveAsync(
        LocalGameSession session,
        AiDifficulty difficulty,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.PendingPromotion is not null)
        {
            throw new InvalidOperationException("A promotion choice is still pending, so the computer player cannot move yet.");
        }

        if (session.IsComplete)
        {
            throw new InvalidOperationException("The game session has already ended, so no computer move can be requested.");
        }

        var response = await _engineClient.GetBestMoveAsync(
            new StockfishEngineRequest(
                StockfishFenSerializer.ToFen(session.Game),
                StockfishDifficultyProfileCatalog.Get(difficulty)),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.BestMoveNotation) ||
            string.Equals(response.BestMoveNotation, "(none)", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stockfish did not return a best move for the current position.");
        }

        ChessMove move;

        try
        {
            move = ChessMove.Parse(response.BestMoveNotation);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Stockfish returned an invalid move notation '{response.BestMoveNotation}'.",
                exception);
        }

        if (!session.Game.IsLegalMove(move))
        {
            throw new InvalidOperationException($"Stockfish returned illegal move '{move}' for the requested position.");
        }

        return move;
    }
}