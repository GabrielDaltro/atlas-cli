namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestPipelineStepLog(
    string StepUuid,
    string StepName,
    string State,
    string Log);
