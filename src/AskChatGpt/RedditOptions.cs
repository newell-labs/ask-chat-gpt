using System.ComponentModel.DataAnnotations;

namespace ChatGptRedditBot;

internal class RedditOptions
{
    [Required]
    public string AppId { get; set; } = default!;

    [Required]
    public string AppSecret { get; set;} = default!;

    [Required]
    public string RefreshToken { get; set;} = default!;
}
