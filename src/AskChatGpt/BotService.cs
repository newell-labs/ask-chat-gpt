using System.Collections.Concurrent;
using AskChatGpt.Chat;
using Reddit.Things;

namespace AskChatGpt;

internal class BotService
{
    private readonly string _signature;
    private readonly ConcurrentDictionary<string, int> _numFailures = new();

    private readonly RedditService _reddit;
    private readonly ChatService _chat;
    private readonly ILogger<RedditService> _logger;

    public BotService(RedditService reddit, ChatService chat, ILogger<RedditService> logger)
    {
        _reddit = reddit;
        _chat = chat;
        _logger = logger;

        var versionWithoutBuildMetadata = ThisAssembly.AssemblyInformationalVersion.Split('+').FirstOrDefault();
        _signature = $"""
            -----
            AskChatGpt v{versionWithoutBuildMetadata ?? "?"} |
            Details & Feedback - see AskChatGPTBot subreddit
            """;
    }

    public async Task ProcessMessageInbox(CancellationToken cancel)
    {
        var messages = _reddit.GetUnreadMessages();

        if (messages.Count == 0)
        {
            _logger.LogTrace("No new messages");
            return;
        }

        _logger.LogInformation("Processing {} messages", messages.Count);

        foreach (var message in messages)
        {
            if (cancel.IsCancellationRequested) break;

            if (_numFailures.TryGetValue(message.Fullname, out var numFailures) && numFailures >= 3)
            {
                _logger.LogWarning("Skipping message '{}' because it has failed too many times", message.Fullname);
                continue;
            }

            try
            {
                await ProcessMessage(message);
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

    private async Task ProcessMessage(Message message)
    {
        _logger.LogDebug("Processing message {}", message.Fullname);
        _logger.LogTrace("Message from {}:\n{}", message.Author, message.Body);

        var username = _reddit.Username;
        var chat = _reddit.GetChatForMessage(message);

        if (chat == null)
        {
            _logger.LogWarning("Could not resolve chat for message {}", message.Fullname);
            return;
        }

        var response = await _chat.GetResponseForChat(username, chat);
        var fullReply = $"{response}\n\n{_signature}";

        var reply = new ChatNode("", chat.Thread, chat, username, fullReply);
        await _reddit.PostReplyForChat(reply);

        await _reddit.MarkMessageRead(message);
    }
}
