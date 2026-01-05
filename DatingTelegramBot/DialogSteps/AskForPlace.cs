using DatingTelegramBot.Models;
using DatingTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DatingTelegramBot.DialogSteps;

public class AskForPlace : IDialogStep
{
    private readonly GeoService _geo;
    public DialogState State => DialogState.WaitingForPlace;

    public AskForPlace(GeoService geo)
    {
        _geo = geo;
    }
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Message message, CancellationToken ct)
    {
        var coords = await _geo.GeocodeAsync(message.Text, ct);

        if (coords != null)
        {
            session.Latitude = coords.Value.lat;
            session.Longitude = coords.Value.lon;
            
            session.State = DialogState.None;

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ясно, {session.Name}. Тебе {session.Age} лет. И ты из {session.Latitude}, {session.Longitude}",
                cancellationToken: ct
            );
        }
        else
        {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"Место не найдено. ${session.Name}, попробуйте переформулировать.",
                cancellationToken: ct
            );
        }
    }
}