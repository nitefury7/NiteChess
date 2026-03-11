namespace NiteChess.Application.GameSessions;

public interface IGameSessionPersistenceService
{
    string Save(LocalGameSession session);

    LocalGameSession Load(string serializedSession);
}