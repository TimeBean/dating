using DatingContracts;
using DatingTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingTelegramBot.DialogSteps;

public class AskForAddPictures : IDialogStep
{
    public DialogState State => DialogState.WaitingForPictures;

    public async Task HandleAsync(ITelegramBotClient bot, UserSession session, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.CallbackQuery)
            return;

        if (update.CallbackQuery!.Data == "AddImagesAgree")
        {
            session.State = DialogState.WaitingForDescription;

            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);

            await bot.SendMessage(
                update.CallbackQuery.From.Id,
                $"Тогда, введите описание.",
                cancellationToken: ct
            );
        }
        else if (update.CallbackQuery.Data == "AddImagesDisagree")
        {
            session.State = DialogState.WaitingForAddPictures;

            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);

            await bot.SendMessage(
                update.CallbackQuery.From.Id,
                $"Хотите добавить фото (1-3)?",
                cancellationToken: ct
            );
        }
    }
}
