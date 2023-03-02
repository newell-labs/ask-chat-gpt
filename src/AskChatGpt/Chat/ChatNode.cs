namespace AskChatGpt.Chat;

public record ChatNode(
    string ID,
    ChatNode? Parent,
    string Author,
    string Body)
{
    public IEnumerable<ChatNode> EnumerateFromFirstInvolvementOfUser(string user)
    {
        return EnumerateFromFirstInvolvementOfUser(this, user, Author == user);
    }

    public static IEnumerable<ChatNode> EnumerateFromFirstInvolvementOfUser(ChatNode node, string user, bool expectSameUser)
    {
        if (expectSameUser && node.Author != user) yield break;

        if (node.Parent != null)
        {
            var parentChain = EnumerateFromFirstInvolvementOfUser(node.Parent, user, !expectSameUser);
            foreach (var n in parentChain) yield return n;
        }

        yield return node;
    }
}
