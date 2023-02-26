namespace ChatGptRedditBot;

internal class Worker : BackgroundService
{
    private readonly OpenAIService _openAIService;
    private readonly RedditService _redditService;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, RedditService redditService, OpenAIService openAIService)
    {
        _logger = logger;
        _redditService = redditService;
        _openAIService = openAIService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                await _redditService.ProcessMessages(_openAIService.GetChatResponse, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing messages");
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}
