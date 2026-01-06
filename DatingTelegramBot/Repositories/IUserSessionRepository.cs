using DatingTelegramBot.Models;

namespace DatingTelegramBot.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> GetOrCreate(long chatId);

    Task Update(UserSession session);
}