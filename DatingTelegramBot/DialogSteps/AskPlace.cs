using DatingContracts;
using DatingTelegramBot.Models;
using DatingTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingTelegramBot.DialogSteps;

public class AskPlace : IDialogStep
{
    private readonly GeoService _geo;
    public DialogState State => DialogState.WaitingForPlace;

    public AskPlace(GeoService geo)
    {
        _geo = geo;
    }
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message)
            return;
        
        var coords = await _geo.GeocodeAsync(update.Message!.Text!, ct);

        if (coords != null)
        {
            session.Latitude = coords.Value.lat;
            session.Longitude = coords.Value.lon;
            
            session.State = DialogState.WaitingForAddDescription;

            await bot.SendMessage(
                chatId: update.Message.Chat.Id,
                text: $"Так и запишем - {session.Latitude}, {session.Longitude}!\nХочешь добавить описание?",
                replyMarkup: new InlineKeyboardMarkup()
                { 
                    InlineKeyboard = new List<IEnumerable<InlineKeyboardButton>>()
                    {
                        new []
                        {
                            new InlineKeyboardButton("Да", "AddDescriptionAgree"),
                            new InlineKeyboardButton("Нет", "AddDescriptionDisagree")
                        }
                    }
                }, 
                cancellationToken: ct
            );
        }
        else
        {
            await bot.SendMessage(
                chatId: update.Message.Chat.Id,
                text: $"Место не найдено. ${session.Name}, попробуйте переформулировать.",
                cancellationToken: ct
            );
        }
    }
}