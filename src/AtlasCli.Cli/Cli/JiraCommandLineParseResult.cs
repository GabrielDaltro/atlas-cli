namespace AtlasCli.Cli;

public sealed record JiraCommandLineParseResult(
    bool IsHelp,
    JiraCommandOptions? Options,
    CliError? Error,
    OutputFormat OutputFormat);
