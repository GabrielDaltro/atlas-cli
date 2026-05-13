namespace AtlasCli.Application.Bitbucket;

public interface IPullRequestSourceCommitResolver
{
    Task<string> GetSourceCommitAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);
}
