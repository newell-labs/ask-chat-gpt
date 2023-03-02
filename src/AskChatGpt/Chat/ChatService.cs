using Microsoft.Extensions.Caching.Memory;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace AskChatGpt.Chat;

internal class ChatService
{
    private readonly IOpenAIService _openAI;
    private readonly IMemoryCache _memoryCache;

    public ChatService(IOpenAIService openAI, IMemoryCache memoryCache)
    {
        _openAI = openAI;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetResponseForChat(string botUser, ChatNode chat)
    {
        var systemPrompt = $"""
            You are a reddit bot digital assistant named /u/{botUser} and have been mentioned on a thread
            titled "{chat.Thread.Title}". Answer with a concise reply.
            """;

        var messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem(systemPrompt)
        };

        foreach (var chatNode in chat.EnumerateFromFirstInvolvementOfUser(botUser))
        {
            var role = chatNode.Author == botUser ?
                StaticValues.ChatMessageRoles.Assistant :
                StaticValues.ChatMessageRoles.User;

            messages.Add(new ChatMessage(role, chatNode.Body));
        }

        var request = new ChatCompletionCreateRequest
        {
            Messages = messages,
            Model = Models.ChatGpt3_5Turbo,
            MaxTokens = 1000
        };

        var response = await _openAI.ChatCompletion.CreateCompletion(request);

        if (!response.Successful)
        {
            throw new Exception($"Error from OpenAI: [{response.Error?.Code}] {response.Error?.Message}");
        }

        return response.Choices.First().Message.Content;
    }
}
