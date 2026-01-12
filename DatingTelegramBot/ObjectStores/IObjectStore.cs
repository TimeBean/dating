using DatingTelegramBot.Models;

namespace DatingTelegramBot.ObjectStores;

public interface IObjectStore
{
    public Task<List<FileStream>> Get(UserSession userSession, CancellationToken ct);
    public Task<Guid> Put(long userId, Stream stream, CancellationToken ct);
}