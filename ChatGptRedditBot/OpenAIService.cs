using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace ChatGptRedditBot;

internal class OpenAIService
{
    private readonly IOpenAIService _openAI;

    public OpenAIService(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<string> GetChatResponse(string author, string message)
    {
        message = message.Replace("u/ask-chat-gpt", "");

        var prompt = $"""
            Add a witty or insightful comment to this reddit thread.

            {author}: {message}
              Comment:
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
