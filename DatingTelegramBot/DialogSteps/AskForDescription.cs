using DatingContracts;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingTelegramBot.DialogSteps;

public class AskForDescription : IDialogStep
{
    public DialogState State => DialogState.WaitingForDescription;
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message)
            return;
        
        session.Description = update.Message!.Text;
        
        session.State = DialogState.WaitingForPictures;
        
        await bot.SendMessage(
            update.Message!.Chat.Id,
            $"Хотите добавить фото (1-3)?",
            replyMarkup: new InlineKeyboardMarkup()
            {
                InlineKeyboard = new List<IEnumerable<InlineKeyboardButton>>()
                {
                    new []
                    {
                        new InlineKeyboardButton("Да", "AddDescriptionAgree"),
                        new InlineKeyboardButton("Нет", "AddDescriptionDisagree"),
                    }
                }
            },
            cancellationToken: ct
        );
    }
}