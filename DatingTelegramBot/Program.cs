using DatingAPIWrapper;
using DatingAPIWrapper.Options;
using DatingTelegramBot.DialogSteps;
using DatingTelegramBot.Handlers;
using DatingTelegramBot.Repositories;
using DatingTelegramBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        configurationBuilder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var token = context.Configuration["TG_TOKEN"] ?? context.Configuration["Token"];

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Telegram token not found.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        services.Configure<WrapperOption>(
            context.Configuration.GetSection("DatingApi")
        );
        services.AddHttpClient<Wrapper>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WrapperOption>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IUserSessionRepository, DatabaseSessionRepository>();
        services.AddHostedService<TelegramBotHostedService>();
        services.AddHttpClient();
        services.AddSingleton<GeoService>();
        services.AddSingleton<IMessageHandler, DialogHandler>();
        services.AddSingleton<IDialogStep, AskNameStep>();
        services.AddSingleton<IDialogStep, AskAgeStep>();
        services.AddSingleton<IDialogStep, AskForPlace>();
    })
    .ConfigureLogging(l => l.AddConsole())
    .Build();

await host.RunAsync();