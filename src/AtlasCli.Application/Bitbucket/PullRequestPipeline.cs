namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestPipeline(
    string Uuid,
    int? BuildNumber,
    string State,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? CompletedAt);
