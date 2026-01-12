using DatingContracts;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DatingTelegramBot.Commands;

public class StartCommand : ICommand
{
    public string CommandToken => "start";
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        session = new UserSession
        {
            ChatId = update.Message!.Chat.Id,
            State = DialogState.WaitingForName
        };

        await bot.SendMessage(
            update.Message!.Chat.Id,
            "Давай знакомиться, как тебя зовут?",
            cancellationToken: ct
        );
    }
}