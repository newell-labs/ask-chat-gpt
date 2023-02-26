using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Reddit;
using Reddit.Controllers.EventArgs;
using Reddit.Things;
using static OpenAI.GPT3.ObjectModels.Models;

namespace ChatGptRedditBot;

internal partial class RedditService : IDisposable
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

    public async Task ProcessMessages(Func<string, string, Task<string>> responseFactory, CancellationToken cancel)
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

            const string username = "u/ask-chat-gpt";
            var messageText = message.Body;
            var usernameIndex = messageText.IndexOf(username);
            if (usernameIndex >= 0)
            {
                var messageStart = usernameIndex + username.Length;
                messageText = messageText[messageStart..];
            }

            if (message.Subreddit != null)
            {
                _logger.LogDebug("Sending reply for message from subreddit {}", message.Subreddit);

                Match match = CommentPermalinkRegex().Match(message.Context);

                string postFullname = "t3_" + (match != null && match.Groups != null && match.Groups.Count >= 2
                    ? match.Groups[1].Value
                    : "");
                if (postFullname.Equals("t3_"))
                {
                    throw new Exception("Unable to extract ID from permalink.");
                }

                var commentFullName = "t1_" + (match != null && match.Groups != null && match.Groups.Count >= 4
                    ? match.Groups[3].Value
                    : "");
                if (commentFullName.Equals("t1_"))
                {
                    throw new Exception("Unable to extract ID from permalink.");
                }

                var comment = _reddit.Comment(commentFullName).About();

                var body = await responseFactory(message.Author, messageText);

                _logger.LogInformation("Sending reply to {}:\n{}", message.Context, body);

                await comment.ReplyAsync(body);

                await _reddit.Account.Messages.ReadMessageAsync(message.Name);
            }
            else
            {
                _logger.LogWarning("Ignoring private message for now");
                continue;

                //_logger.LogDebug("Sending reply for private message");
                //var recipient = message.Author;
                //var subject = $"Re: {message.Subject}";

                //var body = await responseFactory(message.Author, messageText);

                //_logger.LogTrace("Sending reply to {}:\n{}\n{}", recipient, subject, body);
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

    [GeneratedRegex("\\/comments\\/([a-z0-9]+)\\/([-_A-Za-z0-9]+)\\/([a-z0-9]+)\\/")]
    private static partial Regex CommentPermalinkRegex();
}
