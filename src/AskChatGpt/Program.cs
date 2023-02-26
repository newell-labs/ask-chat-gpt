using System.Runtime.InteropServices;
using AskChatGpt;
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
            o.FormatterName = ConsoleFormatterNames.Json;
        });

        services.AddOptions<RedditOptions>().BindConfiguration("Reddit").ValidateDataAnnotations();
        services.AddSingleton<RedditClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedditOptions>>().Value;
            var ua = $"{RuntimeInformation.FrameworkDescription}:chat-gpt-reddit-bot:v0.0.1 (by /u/pb7280)";

            return new RedditClient(opts.AppId, opts.RefreshToken, opts.AppSecret, userAgent: ua);
        });

        services.AddSingleton<RedditService>();

        services.AddOpenAIService();
        services.AddSingleton<OpenAIService>();

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
