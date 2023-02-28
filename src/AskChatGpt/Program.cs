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
            o.FormatterName = ConsoleFormatterNames.Simple;
        });
        services.Configure<SimpleConsoleFormatterOptions>(o =>
        {
            o.SingleLine = true;
        });

        services.AddMemoryCache();

        services.AddOptions<AskChatGptOptions>().BindConfiguration("AskChatGpt");

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
        services.AddSingleton<OpenAIService>();

        services.AddHostedService<Worker>();
    })
    .Build();

var opts = host.Services.GetRequiredService<IOptions<AskChatGptOptions>>().Value;

if (opts.PromptMode)
{
    await RunPromptMode();
}
else
{
    await host.RunAsync();
}

async Task RunPromptMode()
{
    var openAIService = host.Services.GetRequiredService<OpenAIService>();

    var botName = ThisAssembly.AssemblyTitle;
    var author = Environment.UserName;

    while (true)
    {
        Console.WriteLine("Write the reddit comment and press enter");

        var comment = Console.ReadLine();
        if (comment == null) continue;

        var reply = await openAIService.GetChatResponse(botName, author, comment, null);

        Console.WriteLine();
        Console.WriteLine("Reply:");
        Console.WriteLine(reply);
        Console.WriteLine();
    }
}
