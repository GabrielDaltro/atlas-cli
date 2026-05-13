namespace AtlasCli.Application.Bitbucket;

public sealed class PullRequestApplicationService
{
    private readonly IBitbucketPullRequestGateway _gateway;
    private readonly IPullRequestSourceCommitResolver _sourceCommitResolver;

    public PullRequestApplicationService(
        IBitbucketPullRequestGateway gateway,
        IPullRequestSourceCommitResolver sourceCommitResolver)
    {
        _gateway = gateway;
        _sourceCommitResolver = sourceCommitResolver;
    }

    public Task<IReadOnlyList<PullRequestComment>> GetCommentsAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        return _gateway.GetPullRequestCommentsAsync(pullRequest, cancellationToken);
    }

    public Task<IReadOnlyList<PullRequestTask>> GetTasksAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        return _gateway.GetPullRequestTasksAsync(pullRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<PullRequestReport>> GetReportsAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var sourceCommit = await _sourceCommitResolver.GetSourceCommitAsync(pullRequest, cancellationToken);
        return await _gateway.GetCommitReportsAsync(pullRequest, sourceCommit, cancellationToken);
    }
}
