namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReference(string Workspace, string Repository, int Number);
