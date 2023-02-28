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
            As a reddit bot named Ask ChatGPT, with username /u/{botName}, you have been mentioned or replied to
            on a thread.
            Write a reply comment responding to /u/{author}.

            Example Thread:

            /u/redditor: How can I write hello world in python?

            /u/{botName}: Writing hello world in a python program can be done with `print('Hello, World!')`

            Current Thread:

            /u/{parentAuthor ?? author}: {message}

            {(parentAuthor == null ? "" : $"/u/{author}: /u/{botName}")}

            /u/{botName}:
            """;

        var completionResult = await _openAI.Completions.CreateCompletion(new CompletionCreateRequest()
        {
            Prompt = prompt,
            Model = Models.TextCurieV1,
            MaxTokens = 1000
        });

        if (!completionResult.Successful || completionResult.Choices.Count <= 0)
        {
            throw new Exception($"Couldn't get completion: [{completionResult.Error?.Code}] {completionResult.Error?.Message}");
        }

        return completionResult.Choices.First().Text;
    }
}
