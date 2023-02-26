using Reddit;

namespace AskChatGpt;

internal partial class RedditService
{
    private readonly string _signature;

    private readonly RedditClient _reddit;
    private readonly ILogger<RedditService> _logger;

    public RedditService(RedditClient reddit, ILogger<RedditService> logger)
    {
        _reddit = reddit;
        _logger = logger;

        var versionWithoutBuildMetadata = ThisAssembly.AssemblyInformationalVersion.Split('+').FirstOrDefault();
        _signature = $"""
            ^(AskChatGpt v{versionWithoutBuildMetadata ?? "?"} |
            I'm an experimental bot that leverages [OpenAI's text completion API](https://platform.openai.com/docs/guides/completion)
            to respond to mentions or replies. See my [GitHub](https://github.com/newell-labs/chat-gpt-reddit-bot) for more info.)
            """;
    }

    public async Task ProcessMessages(Func<string, string, Task<string>> responseFactory, CancellationToken cancel)
    {
        // We're using the message inbox as a processing queue here, so don't mark them as read just yet.
        // A message is only marked read after it's successfully processed, so failures will retry in the next round.
        // TODO: Add some sort of backoff for retrying persistent failed responses.
        var unreadMessages = _reddit.Account.Messages.GetMessagesUnread(mark: false);

        if (unreadMessages.Count == 0)
        {
            _logger.LogInformation("No new messages");
            return;
        }

        _logger.LogInformation("Processing {} messages", unreadMessages.Count);

        foreach (var message in unreadMessages)
        {
            if (cancel.IsCancellationRequested) break;

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

            if (message.Subreddit != null)
            {
                _logger.LogDebug("Sending reply for message from subreddit {}", message.Subreddit);

                var commentFullName = message.Name;
                var comment = _reddit.Comment(commentFullName).About();

                var author = message.Author;
                string? promptContext = null;
                if (string.IsNullOrEmpty(messageText))
                {
                    // Get prompt from parent comment
                    if (comment.ParentId == null) throw new Exception("No prompt is after the username mention, and no parent comment exists");

                    var parentComment = _reddit.Comment(comment.ParentId).About();
                    author = parentComment.Author;
                    promptContext = parentComment.Body;
                    messageText = promptContext;
                }

                var response = await responseFactory(author, messageText);

                var body = $"{response}\n\n{_signature}";

                if (promptContext != null)
                {
                    body = $">{promptContext}\n{body}";
                }

                _logger.LogInformation("Sending reply to {}:\n------------\n{}\n------------", message.Context, body);

                await comment.ReplyAsync(body);

                await _reddit.Account.Messages.ReadMessageAsync(message.Name);
            }
            else
            {
                _logger.LogWarning("Ignoring private message for now until probation lifted");
                continue;

                //_logger.LogDebug("Sending reply for private message");
                //var recipient = message.Author;
                //var subject = $"Re: {message.Subject}";

                //var body = await responseFactory(message.Author, messageText);

                //_logger.LogTrace("Sending reply to {}:\n{}\n{}", recipient, subject, body);
                //await _reddit.Account.Messages.ComposeAsync(recipient, subject, body);
            }
        }

        if (cancel.IsCancellationRequested)
            _logger.LogWarning("Processing cancelled");
        else
            _logger.LogInformation("Done processing");
    }
}
