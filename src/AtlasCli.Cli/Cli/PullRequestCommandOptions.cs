namespace AtlasCli.Cli;

public sealed record PullRequestCommandOptions(
    PullRequestCommandKind Command,
    string? Repository,
    string? PullRequest,
    string? Url,
    bool IncludeSystem,
    bool LatestCommitPipeline,
    int? BuildNumber,
    OutputFormat OutputFormat)
{
    public string TokenEnvironmentSuffix => Command switch
    {
        PullRequestCommandKind.GetComments => "GET_PR_COMMENTS_TOKEN",
        PullRequestCommandKind.GetTasks => "GET_PR_TASKS_TOKEN",
        PullRequestCommandKind.GetReports => "GET_PR_REPORTS_TOKEN",
        PullRequestCommandKind.GetBranches => "GET_PR_BRANCHES_TOKEN",
        PullRequestCommandKind.GetPipelineLog => "GET_PR_PIPELINE_LOG_TOKEN",
        _ => throw new InvalidOperationException($"Comando {Command} nao possui token configurado.")
    };
}
