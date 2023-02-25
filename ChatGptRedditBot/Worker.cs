namespace ChatGptRedditBot;

internal class Worker : BackgroundService
{
    private readonly RedditService _redditService;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, RedditService redditService)
    {
        _logger = logger;
        _redditService = redditService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _redditService.StartMonitoringInbox();

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);

            await _redditService.ProcessMessages(stoppingToken);

            await Task.Delay(10000, stoppingToken);
        }
    }
}
