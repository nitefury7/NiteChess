namespace NiteChess.Domain.Chess;

public readonly record struct ChessPosition
{
    public ChessPosition(int file, int rank)
    {
        if ((uint)file > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(file), file, "File must be between 0 and 7.");
        }

        if ((uint)rank > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be between 0 and 7.");
        }

        File = file;
        Rank = rank;
    }

    public int File { get; }

    public int Rank { get; }

    public static ChessPosition Parse(string notation)
    {
        if (!TryParse(notation, out var position))
        {
            throw new ArgumentException($"'{notation}' is not a valid board position.", nameof(notation));
        }

        return position;
    }

    public static bool TryParse(string? notation, out ChessPosition position)
    {
        position = default;

        if (string.IsNullOrWhiteSpace(notation) || notation.Length != 2)
        {
            return false;
        }

        var file = char.ToLowerInvariant(notation[0]) - 'a';
        var rank = notation[1] - '1';

        if ((uint)file > 7 || (uint)rank > 7)
        {
            return false;
        }

        position = new ChessPosition(file, rank);
        return true;
    }

    public bool TryOffset(int fileDelta, int rankDelta, out ChessPosition position)
    {
        var file = File + fileDelta;
        var rank = Rank + rankDelta;

        if ((uint)file > 7 || (uint)rank > 7)
        {
            position = default;
            return false;
        }

        position = new ChessPosition(file, rank);
        return true;
    }

    public override string ToString()
    {
        return $"{(char)('a' + File)}{Rank + 1}";
    }

    internal int ToIndex()
    {
        return (Rank * 8) + File;
    }

    internal static ChessPosition FromIndex(int index)
    {
        return new ChessPosition(index % 8, index / 8);
    }
}