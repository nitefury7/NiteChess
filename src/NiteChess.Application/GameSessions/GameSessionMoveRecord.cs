using NiteChess.Domain.Chess;

namespace NiteChess.Application.GameSessions;

public sealed class GameSessionMoveRecord
{
    public GameSessionMoveRecord(
        int plyNumber,
        int turnNumber,
        ChessColor player,
        ChessMove move,
        ChessPiece piece,
        ChessPiece? capturedPiece,
        bool isEnPassant,
        bool isCastling,
        ChessGameStatus resultingStatus)
    {
        if (plyNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(plyNumber), plyNumber, "Ply number must be at least 1.");
        }

        if (turnNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNumber), turnNumber, "Turn number must be at least 1.");
        }

        PlyNumber = plyNumber;
        TurnNumber = turnNumber;
        Player = player;
        Move = move;
        Piece = piece;
        CapturedPiece = capturedPiece;
        IsEnPassant = isEnPassant;
        IsCastling = isCastling;
        ResultingStatus = resultingStatus;
    }

    public int PlyNumber { get; }

    public int TurnNumber { get; }

    public ChessColor Player { get; }

    public ChessMove Move { get; }

    public ChessPiece Piece { get; }

    public ChessPiece? CapturedPiece { get; }

    public bool IsCapture => CapturedPiece is not null;

    public bool IsEnPassant { get; }

    public bool IsCastling { get; }

    public PieceType? PromotionPieceType => Move.PromotionPieceType;

    public string CoordinateNotation => Move.ToString();

    public string DisplayText => Player == ChessColor.White
        ? $"{TurnNumber}. {CoordinateNotation}"
        : $"{TurnNumber}... {CoordinateNotation}";

    public ChessGameStatus ResultingStatus { get; }
}