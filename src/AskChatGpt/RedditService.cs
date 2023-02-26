using System.Collections.Concurrent;
using Reddit;
using Reddit.Things;

namespace AskChatGpt;

internal partial class RedditService
{
    private readonly string _signature;
    private readonly ConcurrentDictionary<string, int> _numFailures = new();

    private readonly RedditClient _reddit;
    private readonly ILogger<RedditService> _logger;

    public RedditService(RedditClient reddit, ILogger<RedditService> logger)
    {
        _reddit = reddit;
        _logger = logger;

        var versionWithoutBuildMetadata = ThisAssembly.AssemblyInformationalVersion.Split('+').FirstOrDefault();
        _signature = $"""
            -----
            AskChatGpt v{versionWithoutBuildMetadata ?? "?"} |
            [Feedback](https://www.reddit.com/message/compose/?to={_reddit.Account.Me.Name})

            ^(Disclaimer: this is not full ChatGPT, as APIs are not available for that yet. It's a more rudimentary
            and won't give as detailed answers.)
            """;
    }

    public async Task ProcessMessages(Func<string, string, string?, Task<string>> responseFactory, CancellationToken cancel)
    {
        // We're using the message inbox as a processing queue here, so don't mark them as read just yet.
        // A message is only marked read after it's successfully processed, so failures will retry in the next round.
        // TODO: Add some sort of backoff for retrying persistent failed responses.
        var unreadMessages = _reddit.Account.Messages.GetMessagesUnread(mark: false);

        if (unreadMessages.Count == 0)
        {
            _logger.LogTrace("No new messages");
            return;
        }

        _logger.LogInformation("Processing {} messages", unreadMessages.Count);

        foreach (var message in unreadMessages)
        {
            if (cancel.IsCancellationRequested) break;

            if (_numFailures.TryGetValue(message.Fullname, out var numFailures) && numFailures >= 3)
            {
                _logger.LogWarning("Skipping message '{}' because it has failed too many times", message.Fullname);
                continue;
            }

            try
            {
                await ProcessMessage(message, responseFactory);
            }
            catch (Exception e)
            {
                _numFailures.AddOrUpdate(message.Fullname, _ => 1, (_, previousFailures) => previousFailures + 1);
                _logger.LogError(e, "Error processing message '{}'", message.Fullname);
            }
        }

        if (cancel.IsCancellationRequested)
            _logger.LogWarning("Processing cancelled");
        else
            _logger.LogInformation("Done processing");
    }

    private async Task ProcessMessage(Message message, Func<string, string, string?, Task<string>> responseFactory)
    {
        _logger.LogDebug("Processing message {}", message.Fullname);
        _logger.LogTrace("Message from {}:\n{}", message.Author, message.Body);

        var username = _reddit.Account.Me.Name;
        var messageText = message.Body;

        var usernameIndex = messageText.IndexOf(username);
        if (usernameIndex >= 0)
        {
            var messageStart = usernameIndex + username.Length;
            messageText = messageText[messageStart..];
        }

        if (message.Subreddit == null)
        {
            _logger.LogWarning("Ignoring private message for now until probation lifted");
            return;
        }

        _logger.LogDebug("Sending reply for message from subreddit {}", message.Subreddit);

        var commentFullName = message.Name;
        var comment = _reddit.Comment(commentFullName).About();

        var parentAuthor = message.Author;
        if (string.IsNullOrEmpty(messageText))
        {
            // Get prompt from parent comment
            if (comment.ParentId == null) throw new Exception("No prompt is after the username mention, and no parent comment exists");

            var parentComment = _reddit.Comment($"t1_{comment.ParentId}").About();
            parentAuthor = parentComment.Author;
            messageText = parentComment.Body;
        }

        var response = await responseFactory(message.Author, messageText, parentAuthor);

        var body = $"{response}\n\n{_signature}";

        _logger.LogInformation("Sending reply to {}:\n------------\n{}\n------------", message.Context, body);

        await comment.ReplyAsync(body);

        await _reddit.Account.Messages.ReadMessageAsync(message.Name);
    }
}
