namespace AtlasCli.Cli;

public sealed record PullRequestCommandOptions(
    PullRequestCommandKind Command,
    string? Repository,
    string? PullRequest,
    string? Url,
    bool IncludeSystem,
    OutputFormat OutputFormat)
{
    public string TokenEnvironmentSuffix => Command switch
    {
        PullRequestCommandKind.GetComments => "GET_PR_COMMENTS_TOKEN",
        PullRequestCommandKind.GetTasks => "GET_PR_TASKS_TOKEN",
        PullRequestCommandKind.GetReports => "GET_PR_REPORTS_TOKEN",
        PullRequestCommandKind.GetBranches => "GET_PR_BRANCHES_TOKEN",
        _ => throw new InvalidOperationException($"Comando {Command} nao possui token configurado.")
    };
}
