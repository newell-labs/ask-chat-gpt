using Microsoft.Extensions.Caching.Memory;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace AskChatGpt;

internal class OpenAIService
{
    private readonly IOpenAIService _openAI;
    private readonly IMemoryCache _memoryCache;

    public OpenAIService(IOpenAIService openAI, IMemoryCache memoryCache)
    {
        _openAI = openAI;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetChatResponse(string botName, string author, string message, string? parentAuthor)
    {
        var response = await _memoryCache.GetOrCreateAsync((author, message, parentAuthor), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await GetChatResponseFromSource(botName, author, message, parentAuthor);
        });

        return response!;
    }

    private async Task<string> GetChatResponseFromSource(string botName, string author, string message, string? parentAuthor)
    {
        var prompt = $"""
            You are a reddit bot named Ask ChatGPT, with username /u/{botName}, and you have been mentioned or replied to on a thread.
            Write a reply comment, attempting to answer any questions or requests from the other redditors.

            Example Thread:

            /u/redditor: How can I write hello world in python?
            /u/{botName}: Writing hello world in python is as simple as `print('Hello, World!')`!

            Thread:

            /u/{parentAuthor ?? author}: {message}

            {(parentAuthor == null ? "" : $"/u/{author}")}

            /u/{botName}:
            """;

        var completionResult = await _openAI.Completions.CreateCompletion(new CompletionCreateRequest()
        {
            Prompt = prompt,
            Model = Models.TextCurieV1,
            MaxTokens = 500
        });

        if (!completionResult.Successful || completionResult.Choices.Count <= 0)
        {
            throw new Exception($"Couldn't get completion: [{completionResult.Error?.Code}] {completionResult.Error?.Message}");
        }

        return completionResult.Choices.First().Text;
    }
}
