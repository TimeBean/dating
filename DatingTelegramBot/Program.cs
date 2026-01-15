using DatingAPIWrapper;
using DatingAPIWrapper.Options;
using DatingTelegramBot.Commands;
using DatingTelegramBot.DialogSteps;
using DatingTelegramBot.Handlers;
using DatingTelegramBot.ObjectStores;
using DatingTelegramBot.Repositories;
using DatingTelegramBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cb =>
    {
        cb.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var token = context.Configuration["TG_TOKEN"] ?? context.Configuration["Token"];
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Telegram token missing in configuration.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));

        services.Configure<WrapperOption>(context.Configuration.GetSection("DatingApi"));
        services.AddHttpClient<Wrapper>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WrapperOption>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<IUserSessionRepository, DatabaseSessionRepository>();
        services.AddHostedService<TelegramBotHostedService>();
        services.AddSingleton<GeoService>();
        services.Configure<S3Options>(context.Configuration.GetSection("S3"));
        services.AddSingleton<IObjectStore, MinIoObjectStore>();
        services.AddSingleton<IMessageHandler, DialogHandler>();
        services.AddSingleton<IDialogStep, AskNameStep>();
        services.AddSingleton<IDialogStep, AskAgeStep>();
        services.AddSingleton<IDialogStep, AskPlace>();
        services.AddSingleton<IDialogStep, AskForAddDescription>();
        services.AddSingleton<IDialogStep, AskDescription>();
        services.AddTransient<ICommandHandler, CommandHandler>();
        services.AddTransient<ICommand, StartCommand>();
        services.AddTransient<ICommand, ProfileCommand>();
    })
    .ConfigureLogging(l => l.AddConsole())
    .Build();

await host.RunAsync();