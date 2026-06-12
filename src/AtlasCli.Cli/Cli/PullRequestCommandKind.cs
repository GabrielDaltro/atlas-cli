namespace AtlasCli.Cli;

public enum PullRequestCommandKind
{
    GetComments,
    GetTasks,
    GetReports,
    GetUncoveredLines,
    GetSonarIssues,
    GetBranches,
    GetPipelineLog
}
