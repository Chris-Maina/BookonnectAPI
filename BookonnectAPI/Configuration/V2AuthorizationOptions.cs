namespace BookonnectAPI.Configuration;

public sealed class V2AuthorizationOptions
{
    public const string SectionName = "Authentication:V2Authorization";

    public int[] AdminUserIds { get; set; } = Array.Empty<int>();
}
