using DatingContracts;
using DatingTelegramBot.Exceptions;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingTelegramBot.DialogSteps;

public class AskForAddDescription : IDialogStep
{
    public DialogState State => DialogState.WaitingForAddDescription;
    
    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            if (update.CallbackQuery!.Data == "AddDescriptionAgree")
            {
                session.State = DialogState.WaitingForDescription;
                
                await bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
                
                await bot.SendMessage(
                    update.CallbackQuery.From.Id,
                    $"Тогда, введите описание.",
                    cancellationToken: ct
                );
            }
            else if (update.CallbackQuery.Data == "AddDescriptionDisagree")
            {
                session.State = DialogState.Done;
                
                await bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
                
                await bot.SendMessage(
                    update.CallbackQuery.From.Id,
                    $"Создание профиля завершено.",
                    cancellationToken: ct
                );
            }
        }
    }
}