namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReferenceInput(
    string? Repository,
    string? PullRequest,
    string? Url);
