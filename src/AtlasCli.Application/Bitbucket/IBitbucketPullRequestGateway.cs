namespace AtlasCli.Application.Bitbucket;

public interface IBitbucketPullRequestGateway
{
    Task<IReadOnlyList<PullRequestComment>> GetPullRequestCommentsAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestTask>> GetPullRequestTasksAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<PullRequestBranches> GetPullRequestBranchesAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<string> GetPullRequestSourceCommitAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestReport>> GetCommitReportsAsync(
        PullRequestReference pullRequest,
        string commitHash,
        CancellationToken cancellationToken = default);
}
