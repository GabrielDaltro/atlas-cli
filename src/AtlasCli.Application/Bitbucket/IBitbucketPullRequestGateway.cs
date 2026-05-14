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

    Task<int> GetReferencedPipelineBuildNumberAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<PullRequestPipeline> GetLatestPipelineAsync(
        RepositoryReference repository,
        string commitHash,
        CancellationToken cancellationToken = default);

    Task<PullRequestPipeline> GetPipelineByBuildNumberAsync(
        RepositoryReference repository,
        int buildNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestPipelineStep>> GetPipelineStepsAsync(
        RepositoryReference repository,
        string pipelineUuid,
        CancellationToken cancellationToken = default);

    Task<string> GetPipelineStepLogAsync(
        RepositoryReference repository,
        string pipelineUuid,
        string stepUuid,
        CancellationToken cancellationToken = default);

    Task<string> GetPullRequestSourceCommitAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestReport>> GetCommitReportsAsync(
        PullRequestReference pullRequest,
        string commitHash,
        CancellationToken cancellationToken = default);
}
