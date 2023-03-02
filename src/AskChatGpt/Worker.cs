namespace AskChatGpt;

internal class Worker : BackgroundService
{
    private readonly BotService _botService;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, BotService botService)
    {
        _logger = logger;
        _botService = botService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                await _botService.ProcessMessageInbox(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing messages");
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}
