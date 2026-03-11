namespace NiteChess.Application.GameSessions;

public enum GameSessionMoveOutcome
{
    Applied = 0,
    PromotionSelectionRequired = 1,
    IllegalMove = 2,
    GameAlreadyFinished = 3,
    PendingPromotionSelectionRequired = 4
}