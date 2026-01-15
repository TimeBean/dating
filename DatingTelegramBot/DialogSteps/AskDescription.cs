using DatingContracts;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingTelegramBot.DialogSteps;

public class AskDescription : IDialogStep
{
    public DialogState State => DialogState.WaitingForDescription;
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message)
            return;
        
        session.State = DialogState.None;
        
        session.Description = update.Message!.Text;
        
        await bot.SendMessage(
            update.Message!.Chat.Id,
            $"Создание профиля завершено.",
            cancellationToken: ct
        );
    }
}