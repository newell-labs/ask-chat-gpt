using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace AskChatGpt;

internal class OpenAIService
{
    private readonly IOpenAIService _openAI;

    public OpenAIService(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<string> GetChatResponse(string author, string message, string? parentAuthor)
    {
        const string selfName = "cgptbot";
        var prompt = $"""
            Acting as a reddit bot named {selfName}, reply to this comment,
            giving an insightful answer or witty remark in response.

            {(parentAuthor == null ? "" : $"/u/{author} has asked you to comment on /u/{parentAuthor}'s comment above them.")}

            /u/{parentAuthor ?? author}'s comment: {message}


              /u/{selfName}'s reply:
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
