namespace AtlasCli.Application.Bitbucket;

public interface IPullRequestPipelineBuildResolver
{
    Task<int> GetBuildNumberAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);
}
