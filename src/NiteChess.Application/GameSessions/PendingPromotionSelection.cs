using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public sealed class PendingPromotionSelection
{
    public PendingPromotionSelection(
        ChessPosition from,
        ChessPosition to,
        ChessColor player,
        IReadOnlyList<PieceType> availablePieceTypes)
    {
        ArgumentNullException.ThrowIfNull(availablePieceTypes);

        var options = availablePieceTypes.Distinct().ToArray();

        if (options.Length == 0)
        {
            throw new ArgumentException("At least one promotion choice must be available.", nameof(availablePieceTypes));
        }

        From = from;
        To = to;
        Player = player;
        AvailablePieceTypes = options;
    }

    public ChessPosition From { get; }

    public ChessPosition To { get; }

    public ChessColor Player { get; }

    public IReadOnlyList<PieceType> AvailablePieceTypes { get; }

    public string CoordinateNotation => $"{From}{To}";
}