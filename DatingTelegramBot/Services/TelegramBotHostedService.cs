using DatingContracts;
using DatingTelegramBot.Exceptions;
using DatingTelegramBot.Handlers;
using DatingTelegramBot.Models;
using DatingTelegramBot.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingTelegramBot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly IUserSessionRepository _repository;
        private readonly IMessageHandler _dialogHandler;
        private readonly ICommandHandler _commandHandler;

        public TelegramBotHostedService(ITelegramBotClient bot, ILogger<TelegramBotHostedService> logger,
            IMessageHandler dialogHandler, IUserSessionRepository repository, ICommandHandler commandHandler)
        {
            _bot = bot;
            _logger = logger;
            _dialogHandler = dialogHandler;
            _repository = repository;
            _commandHandler = commandHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bot starting");

            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: new ReceiverOptions(),
                cancellationToken: stoppingToken
            );

            await Task.Delay(-1, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message?.Text?.StartsWith("/") == true:
                    await _commandHandler.HandleAsync(botClient, update, ct);
                    break;

                case UpdateType.Message:
                case UpdateType.CallbackQuery:
                    await _dialogHandler.HandleAsync(botClient, update, ct);
                    break;

                default:
                    throw new UnknownUpdateException();
            }

            _logger.LogInformation("Handled update from {ChatId}",
                update.Message?.Chat.Id ?? update.CallbackQuery?.From.Id);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram polling error");

            return Task.CompletedTask;
        }
    }
}