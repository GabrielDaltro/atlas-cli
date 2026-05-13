namespace AtlasCli.Cli;

public sealed record CommandLineParseResult(
    bool IsHelp,
    PullRequestCommandKind? HelpCommand,
    PullRequestCommandOptions? Options,
    CliError? Error,
    OutputFormat OutputFormat);
