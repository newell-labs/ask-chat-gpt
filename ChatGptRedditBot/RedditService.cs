using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers.EventArgs;
using Reddit.Inputs.LinksAndComments;
using Reddit.Things;

namespace ChatGptRedditBot;

internal class RedditService : IDisposable
{
    private readonly ConcurrentQueue<Message> _unreadMessages = new();

    private readonly RedditClient _reddit;
    private readonly ILogger<RedditService> _logger;

    public RedditService(RedditClient reddit, ILogger<RedditService> logger)
    {
        _reddit = reddit;
        _logger = logger;
    }

    public void StartMonitoringInbox()
    {
        _reddit.Account.Messages.MonitorUnread();
        _reddit.Account.Messages.UnreadUpdated += OnUnreadUpdated;
    }

    public void Dispose()
    {
        _reddit.Account.Messages.UnreadUpdated -= OnUnreadUpdated;
    }

    public async Task ProcessMessages(CancellationToken cancel)
    {
        var currentCount = _unreadMessages.Count;
        if (currentCount == 0)
        {
            _logger.LogInformation("No new messages");
            return;
        }

        _logger.LogInformation("Processing {} messages", currentCount);

        while (_unreadMessages.TryDequeue(out var message) && !cancel.IsCancellationRequested)
        {
            _logger.LogDebug("Processing message {}", message.Fullname);
            _logger.LogTrace("Message from {}:\n{}", message.Author, message.Body);

            if (message.Subreddit != null)
            {
                _logger.LogDebug("Sending reply for message from subreddit {}", message.Subreddit);
                throw new NotImplementedException();
            }
            else
            {
                _logger.LogDebug("Sending reply for private message");
                var recipient = message.Author;
                var subject = $"Re: {message.Subject}";
                var body = "Yes";

                _logger.LogTrace("Sending reply to {}:\n{}\n{}", recipient, subject, body);
                //await _reddit.Account.Messages.ComposeAsync(recipient, subject, body);
            }
        }

        _logger.LogInformation(cancel.IsCancellationRequested ? "Processing cancelled" : "Done processing");
    }

    private void OnUnreadUpdated(object? sender, MessagesUpdateEventArgs e)
    {
        foreach (var message in e.NewMessages)
        {
            _unreadMessages.Enqueue(message);
        }
    }
}
