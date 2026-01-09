using DatingTelegramBot.Models;

namespace DatingTelegramBot.ObjectStores;

public interface IObjectStore
{
    public Task<FileStream> Get(UserSession userSession, CancellationToken ct);
    public Task<List<FileStream>> GetAll(UserSession userSession, CancellationToken ct);
    public Task<string> Put(long userId, FileStream fileStream, CancellationToken ct);
}