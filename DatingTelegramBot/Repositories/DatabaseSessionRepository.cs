using DatingAPIWrapper;
using DatingContracts.Dtos;
using DatingTelegramBot.Models;

namespace DatingTelegramBot.Repositories;

public class DatabaseSessionRepository : IUserSessionRepository
{
    private readonly Wrapper _wrapper;
    
    public DatabaseSessionRepository(Wrapper wrapper)
    {
        _wrapper = wrapper;
    }
    
    public async Task<UserSession> GetOrCreate(long chatId)
    {
        var user = await _wrapper.GetUserAsync(chatId);

        if (user == null)
        {
            user = new UserDto
            {
                ChatId = chatId
            };

            await _wrapper.CreateUserAsync(user);

            return new UserSession()
            {
                ChatId = chatId
            };
        }

        return new UserSession()
        {
            ChatId = chatId,
            Age = user.Age,
            Description = user.Description,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            Name = user.Name,
            State = user.State
        };
    }

    public async Task Update(UserSession session)
    {
        var updateUser = new UpdateUser()
        {
            Age = session.Age,
            Description = session.Description,
            Latitude = session.Latitude,
            Longitude = session.Longitude,
            Name = session.Name,
            State = session.State
        };

        await _wrapper.PatchUserAsync(session.ChatId, updateUser);
    }
}