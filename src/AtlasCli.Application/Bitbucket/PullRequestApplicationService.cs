namespace AtlasCli.Application.Bitbucket;

public sealed class PullRequestApplicationService
{
    private readonly IBitbucketPullRequestGateway _gateway;
    private readonly IPullRequestSourceCommitResolver _sourceCommitResolver;
    private readonly IPullRequestPipelineBuildResolver _pipelineBuildResolver;

    public PullRequestApplicationService(
        IBitbucketPullRequestGateway gateway,
        IPullRequestSourceCommitResolver sourceCommitResolver,
        IPullRequestPipelineBuildResolver pipelineBuildResolver)
    {
        _gateway = gateway;
        _sourceCommitResolver = sourceCommitResolver;
        _pipelineBuildResolver = pipelineBuildResolver;
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

    public Task<PullRequestBranches> GetBranchesAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        return _gateway.GetPullRequestBranchesAsync(pullRequest, cancellationToken);
    }

    public async Task<PullRequestPipelineLog> GetLatestPipelineLogAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var sourceCommit = await _sourceCommitResolver.GetSourceCommitAsync(pullRequest, cancellationToken);
        var pipeline = await _gateway.GetLatestPipelineAsync(pullRequest.ToRepositoryReference(), sourceCommit, cancellationToken);
        return await BuildPipelineLogAsync(pullRequest.ToRepositoryReference(), pipeline, cancellationToken);
    }

    public async Task<PullRequestPipelineLog> GetReferencedPipelineLogAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var buildNumber = await _pipelineBuildResolver.GetBuildNumberAsync(pullRequest, cancellationToken);
        return await GetPipelineLogByBuildNumberAsync(pullRequest.ToRepositoryReference(), buildNumber, cancellationToken);
    }

    public async Task<PullRequestPipelineLog> GetPipelineLogByBuildNumberAsync(
        RepositoryReference repository,
        int buildNumber,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await _gateway.GetPipelineByBuildNumberAsync(repository, buildNumber, cancellationToken);
        return await BuildPipelineLogAsync(repository, pipeline, cancellationToken);
    }

    private async Task<PullRequestPipelineLog> BuildPipelineLogAsync(
        RepositoryReference repository,
        PullRequestPipeline pipeline,
        CancellationToken cancellationToken)
    {
        var steps = await _gateway.GetPipelineStepsAsync(repository, pipeline.Uuid, cancellationToken);
        var stepLogs = new List<PullRequestPipelineStepLog>(steps.Count);

        foreach (var step in steps)
        {
            var log = await _gateway.GetPipelineStepLogAsync(repository, pipeline.Uuid, step.Uuid, cancellationToken);
            stepLogs.Add(new PullRequestPipelineStepLog(step.Uuid, step.Name, step.State, log));
        }

        return new PullRequestPipelineLog(
            pipeline.Uuid,
            pipeline.BuildNumber,
            pipeline.State,
            pipeline.CreatedAt,
            pipeline.CompletedAt,
            stepLogs);
    }

    public async Task<IReadOnlyList<PullRequestReport>> GetReportsAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var sourceCommit = await _sourceCommitResolver.GetSourceCommitAsync(pullRequest, cancellationToken);
        return await _gateway.GetCommitReportsAsync(pullRequest, sourceCommit, cancellationToken);
    }
}
