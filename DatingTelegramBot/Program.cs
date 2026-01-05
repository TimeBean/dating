using DatingTelegramBot.DialogSteps;
using DatingTelegramBot.Handlers;
using DatingTelegramBot.Repositories;
using DatingTelegramBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        configurationBuilder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var token = context.Configuration["TG_TOKEN"] ?? context.Configuration["TOKEN"];

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Telegram token not found.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        services.AddSingleton<IUserSessionRepository, InMemorySessionRepository>();
        services.AddHostedService<TelegramBotHostedService>();
        services.AddSingleton<IMessageHandler, DialogHandler>();
        services.AddSingleton<IDialogStep, AskNameStep>();
        services.AddSingleton<IDialogStep, AskAgeStep>();
    })
    .ConfigureLogging(l => l.AddConsole())
    .Build();

await host.RunAsync();