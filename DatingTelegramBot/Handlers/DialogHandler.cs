using DatingTelegramBot.DialogSteps;
using DatingTelegramBot.Exceptions;
using DatingTelegramBot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DatingTelegramBot.Handlers;

public class DialogHandler : IMessageHandler
{
    private readonly IEnumerable<IDialogStep> _steps; 
    private readonly IUserSessionRepository _repository;

    public DialogHandler(IEnumerable<IDialogStep> steps, IUserSessionRepository repository)
    {
        _steps = steps;
        _repository = repository;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var session = await _repository.GetOrCreate(message.Chat.Id);

        var step = _steps.FirstOrDefault(s => s.State == session.State);

        if (step == null)
        {
            step = new AskNameStep();
        }
        
        await step.HandleAsync(bot, session, message, ct);

        await _repository.Update(session);
    }
}