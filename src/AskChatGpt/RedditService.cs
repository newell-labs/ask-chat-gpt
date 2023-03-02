﻿using AskChatGpt.Chat;
using Reddit;
using Reddit.Things;

namespace AskChatGpt;

internal partial class RedditService
{
    private readonly RedditClient _reddit;
    private readonly ILogger<RedditService> _logger;

    public RedditService(RedditClient reddit, ILogger<RedditService> logger)
    {
        _reddit = reddit;
        _logger = logger;
    }

    public string Username => _reddit.Account.Me.Name;

    public IReadOnlyList<Message> GetUnreadMessages()
    {
        return _reddit.Account.Messages.GetMessagesUnread(mark: false);
    }

    public Task MarkMessageRead(Message message)
    {
        return _reddit.Account.Messages.ReadMessageAsync(message.Name);
    }

    public ChatMessage? GetChatForMessage(Message message)
    {
        if (message.Subreddit == null)
        {
            _logger.LogWarning("Ignoring private message for now until probation lifted");
            return null;
        }

        var commentFullName = message.Name;
        return BuildChatChainForCommentName(commentFullName);
    }

    public async Task PostReplyForChat(ChatMessage chat)
    {
        var parent = chat.Parent ?? throw new ArgumentException("Parent must not be null", nameof(chat));
        var parentComment = _reddit.Comment(parent.ID).About();

        _logger.LogInformation("Sending reply to {}:\n------------\n{}\n------------", parentComment.Author, chat.Body);

        await parentComment.ReplyAsync(chat.Body);
    }

    private ChatMessage? BuildChatChainForCommentName(string? commentFullName)
    {
        Reddit.Controllers.Comment comment;
        try
        {
            comment = _reddit.Comment(commentFullName).About();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error constructing comment chain, breaking here");
            return null;
        }

        var parent = BuildChatChainForCommentName(comment.ParentFullname);
        return new ChatMessage(comment.Fullname, parent, comment.Author, comment.Body);
    }
}
