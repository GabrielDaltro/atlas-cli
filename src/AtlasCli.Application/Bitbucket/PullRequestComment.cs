namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestComment(
    long Id,
    string Author,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Text,
    string Type,
    string? File,
    int? Line,
    string State,
    string? Url,
    long? ParentId);
