namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestPipelineLog(
    string PipelineUuid,
    int? BuildNumber,
    string State,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<PullRequestPipelineStepLog> Steps);
