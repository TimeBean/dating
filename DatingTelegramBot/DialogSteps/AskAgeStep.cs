using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DatingTelegramBot.DialogSteps;

public class AskAgeStep : IDialogStep
{
    public DialogState State => DialogState.WaitingForAge;

    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Message message, CancellationToken ct)
    {
        session.Age = Int32.Parse(message.Text);

        session.State = DialogState.WaitingForSetupComplite;

        await bot.SendMessage(
            message.Chat.Id,
            $"Ясно, {session.Name}. Тебе {session.Age} лет.",
            cancellationToken: ct
        );
    }
}