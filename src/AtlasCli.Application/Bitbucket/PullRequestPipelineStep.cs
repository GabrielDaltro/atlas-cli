namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestPipelineStep(
    string Uuid,
    string Name,
    string State);
