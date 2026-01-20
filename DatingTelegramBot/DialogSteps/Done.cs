using DatingContracts;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingTelegramBot.DialogSteps;

public class Done : IDialogStep
{
    public DialogState State => DialogState.Done;
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message)
            return;
        
        await bot.SendMessage(
            update.Message!.Chat.Id,
            $"Всё уже настроенно, спасибо!",
            cancellationToken: ct
        );
    }
}