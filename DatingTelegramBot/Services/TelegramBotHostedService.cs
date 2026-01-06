using DatingContracts;
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
        private readonly IMessageHandler _messageHandler;

        public TelegramBotHostedService(ITelegramBotClient bot, ILogger<TelegramBotHostedService> logger, IMessageHandler messageHandler, IUserSessionRepository repository)
        {
            _bot = bot;
            _logger = logger;
            _messageHandler = messageHandler;
            _repository = repository;
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
                if (update.Type == UpdateType.Message)
                {
                    var message = update.Message;
                    _logger.LogInformation("Received from {User}: {Text}", message.From.Username, message.Text);
                    
                    if (update.Message?.Text != null)
                    {
                        if (message!.Text!.Contains("/start") || message.Text.Contains("/reset"))
                        {
                            var session = await _repository.GetOrCreate(message.Chat.Id);

                            if (session.Name != null && !message.Text.Contains("/reset"))
                            {
                                await botClient.SendMessage(
                                    message.Chat.Id,
                                    await GetProfileText(botClient,  message.Chat.Id, ct),
                                    cancellationToken: ct
                                );
                            
                                return;
                            }

                            session.State = DialogState.WaitingForName;
                            session.Name = null;
                            session.Age = null;

                            await _repository.Update(session);

                            await botClient.SendMessage(
                                message.Chat.Id,
                                "Привет! Давай начнем. Как тебя зовут?",
                                cancellationToken: ct
                            );
                            return;
                        }
                        if (message.Text.Contains("/profile"))
                        {
                            await botClient.SendMessage(
                                message.Chat.Id,
                                await GetProfileText(botClient, message.Chat.Id, ct),
                                cancellationToken: ct
                            );
                            return;
                        }
                    }
                }
                
                await _messageHandler.HandleAsync(botClient, update, ct);
                _logger.LogInformation("Handled message from {ChatId}", update.Message?.Chat.Id == null ? update.Message?.Chat.Id  : update.CallbackQuery?.From.Id);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram polling error");

            return Task.CompletedTask;
        }

        private async Task<string> GetProfileText(ITelegramBotClient botClient, long chatId, CancellationToken ct)
        {
            var session = await _repository.GetOrCreate(chatId);

            var profileText = string.Empty;

            profileText +=  "Ваш профиль выглядит так:\n" + 
                           $"{session.Name}, {session.Age} - {session.Description} ";
            
            return profileText;
        }
    }
}