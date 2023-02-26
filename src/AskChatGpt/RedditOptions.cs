using System.ComponentModel.DataAnnotations;

namespace AskChatGpt;

internal class RedditOptions
{
    [Required]
    public string AppId { get; set; } = default!;

    [Required]
    public string AppSecret { get; set;} = default!;

    [Required]
    public string RefreshToken { get; set;} = default!;

    public string? Author { get; set; }
}
