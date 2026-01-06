using System.Collections.Concurrent;
using DatingContracts;
using DatingTelegramBot.Models;

namespace DatingTelegramBot.Repositories;

public class InMemorySessionRepository : IUserSessionRepository
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public async Task<UserSession> GetOrCreate(long chatId)
    {
        return _sessions.GetOrAdd(chatId, id => new UserSession
        {
            ChatId = id,
            State = DialogState.None
        });
    }

    public async Task Update(UserSession session)
    {
        _sessions[session.ChatId] = session;
    }
}