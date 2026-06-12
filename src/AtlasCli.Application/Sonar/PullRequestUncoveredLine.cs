namespace AtlasCli.Application.Sonar;

public sealed record PullRequestUncoveredLine(
    string File,
    int Line,
    string Status,
    string Code);
