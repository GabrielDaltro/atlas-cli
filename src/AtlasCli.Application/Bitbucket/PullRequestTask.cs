namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestTask(
    long Id,
    string Creator,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Text,
    string State,
    bool Pending,
    DateTimeOffset? ResolvedAt,
    string? ResolvedBy,
    long? CommentId,
    string? Url);
