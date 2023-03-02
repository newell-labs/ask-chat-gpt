using System.Runtime.InteropServices;
using AskChatGpt;
using AskChatGpt.Chat;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenAI.GPT3.Extensions;
using Reddit;

Console.WriteLine($"Starting up {ThisAssembly.AssemblyTitle} v{ThisAssembly.AssemblyInformationalVersion}");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) =>
    {
        var configPath = context.Configuration["ConfigPath"];
        if (configPath != null) builder.AddJsonFile(configPath, false, true);
    })
    .ConfigureServices(services =>
    {
        services.Configure<ConsoleLoggerOptions>(o =>
        {
            o.FormatterName = ConsoleFormatterNames.Simple;
        });
        services.Configure<SimpleConsoleFormatterOptions>(o =>
        {
            o.SingleLine = true;
        });

        services.AddMemoryCache();

        services.AddOptions<RedditOptions>().BindConfiguration("Reddit").ValidateDataAnnotations();
        services.AddSingleton<RedditClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedditOptions>>().Value;
            var ua = $"{RuntimeInformation.FrameworkDescription}:chat-gpt-reddit-bot:v{ThisAssembly.AssemblyInformationalVersion}";
            if (opts.Author is not null)
            {
                ua += $" (by /u/{opts.Author})";
            }

            return new RedditClient(opts.AppId, opts.RefreshToken, opts.AppSecret, userAgent: ua);
        });

        services.AddSingleton<RedditService>();

        services.AddOpenAIService();
        services.AddSingleton<ChatService>();

        services.AddSingleton<BotService>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
