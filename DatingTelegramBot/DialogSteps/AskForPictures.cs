using DatingContracts;
using DatingTelegramBot.Models;
using DatingTelegramBot.ObjectStores;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingTelegramBot.DialogSteps;

public class AskForPictures : IDialogStep
{
    public DialogState State => DialogState.WaitingForPictures;

    private readonly IObjectStore _objectStore;

    public AskForPictures(IObjectStore objectStore)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
    }

    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message || update.Message == null)
            return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text?.Trim();

        if (string.Equals(text, "стоп", StringComparison.OrdinalIgnoreCase))
        {
            session.State = DialogState.None;
            return;
        }

        var photos = update.Message.Photo;
        if (photos == null || photos.Length == 0)
        {
            await bot.SendMessage(chatId,
                "Пожалуйста, отправьте фотографии. Если желаете завершить сейчас, напишите: \"стоп\"",
                cancellationToken: ct);
            return;
        }

        session.Pictures ??= new List<Guid>();

        const int maxPhotos = 3;
        if (session.Pictures.Count >= maxPhotos)
        {
            await bot.SendMessage(chatId,
                $"Принял фотографии ({session.Pictures.Count}/{maxPhotos}). Настройка профиля завершена.",
                cancellationToken: ct);
            session.State = DialogState.None;
            return;
        }

        var fileId = photos
            .OrderByDescending(p => p.FileSize ?? 0)
            .First()
            .FileId;

        var tgFile = await bot.GetFile(fileId, ct);

        await using var memoryStream = new MemoryStream();
        await bot.DownloadFile(tgFile.FilePath ?? fileId, memoryStream, ct);
        memoryStream.Position = 0;

        var photoGuid = await _objectStore.Put(session.ChatId, memoryStream, ct);
        session.Pictures.Add(photoGuid);

        var count = session.Pictures.Count;
        var reply = count >= maxPhotos
            ? $"Принял фотографии ({count}/{maxPhotos}). Настройка профиля завершена."
            : $"Принял фотографию ({count}/{maxPhotos})";

        await bot.SendMessage(chatId, reply, cancellationToken: ct);

        if (count >= maxPhotos)
            session.State = DialogState.None;
    }
}