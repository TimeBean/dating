using DatingTelegramBot.Commands;
using DatingTelegramBot.Extensions;
using DatingTelegramBot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DatingTelegramBot.Handlers;

public class CommandHandler : ICommandHandler
{
    private readonly IUserSessionRepository _repository;
    private readonly IReadOnlyDictionary<string, ICommand> _commandsByName;

    public CommandHandler(IUserSessionRepository repository, IEnumerable<ICommand> commands)
    {
        _repository = repository;
        _commandsByName = commands
            .ToDictionary(c => c.CommandToken, StringComparer.OrdinalIgnoreCase)
            .AsReadOnly();
    }

    public async Task HandleAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text?.StartsWith("/") != true)
            return;
        
        var text = update.Message!.Text!.Trim();
        var commandToken = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
            .TrimStart('/')
            .Split('@', StringSplitOptions.RemoveEmptyEntries)[0];

        if (!_commandsByName.TryGetValue(commandToken, out var command))
        {
            await bot.SendMessage(update.GetChatId(), $"Неизвестная команда: /{commandToken}", cancellationToken: ct);
            return;
        }

        var session = await _repository.GetOrCreate(update.GetChatId());

        await command.HandleAsync(bot, session, update, ct);
        await _repository.Update(session);
    }
}