using ChatGptRedditBot;
using Microsoft.Extensions.Options;
using Reddit;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddOptions<RedditOptions>().BindConfiguration("Reddit").ValidateDataAnnotations();
        services.AddSingleton<RedditClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedditOptions>>().Value;

            return new RedditClient(opts.AppId, opts.RefreshToken, opts.AppSecret);
        });

        services.AddSingleton<RedditService>();

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
