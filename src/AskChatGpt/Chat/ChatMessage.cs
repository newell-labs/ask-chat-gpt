namespace AskChatGpt.Chat;

public record ChatMessage(
    string ID,
    ChatMessage? Parent,
    string Author,
    string Body);
