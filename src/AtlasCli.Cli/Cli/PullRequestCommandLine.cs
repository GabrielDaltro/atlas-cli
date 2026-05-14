namespace AtlasCli.Cli;

public static class PullRequestCommandLine
{
    public static CommandLineParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Help(null);
        }

        if (args[0] is "--help" or "-h" or "help")
        {
            return Help(null);
        }

        if (args.Length >= 2 && IsCommand(args[0], "bb") && args[1] is "--help" or "-h" or "help")
        {
            return Help(null);
        }

        PullRequestCommandKind? command = null;
        var index = 0;

        if (TryParseCommand(args, out var parsedCommand, out index))
        {
            command = parsedCommand;
        }
        else if (!args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return Failure($"Comando '{args[0]}' nao reconhecido.", OutputFormat.Table);
        }

        string? repository = null;
        string? pullRequest = null;
        string? url = null;
        var includeSystem = false;
        var latestCommitPipeline = false;
        int? buildNumber = null;
        var outputFormat = OutputFormat.Table;

        for (var i = index; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--help" or "-h")
            {
                return Help(command, outputFormat);
            }

            switch (arg)
            {
                case "--repo":
                    if (!TryReadValue(args, ref i, arg, outputFormat, out repository, out var repoError))
                    {
                        return repoError;
                    }

                    break;

                case "--pr":
                    if (!TryReadValue(args, ref i, arg, outputFormat, out pullRequest, out var prError))
                    {
                        return prError;
                    }

                    break;

                case "--url":
                    if (!TryReadValue(args, ref i, arg, outputFormat, out url, out var urlError))
                    {
                        return urlError;
                    }

                    break;

                case "--include-system":
                    includeSystem = true;
                    break;

                case "--latest-commit-pipeline":
                    latestCommitPipeline = true;
                    break;

                case "--build":
                    if (!TryReadValue(args, ref i, arg, outputFormat, out var buildValue, out var buildError))
                    {
                        return buildError;
                    }

                    if (!int.TryParse(buildValue, out var parsedBuildNumber) || parsedBuildNumber <= 0)
                    {
                        return Failure("--build deve ser um numero positivo.", outputFormat);
                    }

                    buildNumber = parsedBuildNumber;
                    break;

                case "--output":
                    if (!TryReadValue(args, ref i, arg, outputFormat, out var output, out var outputError))
                    {
                        return outputError;
                    }

                    if (!TryParseOutput(output, out outputFormat))
                    {
                        return Failure("--output deve ser 'table' ou 'json'.", outputFormat);
                    }

                    break;

                default:
                    return Failure($"Parametro '{arg}' nao reconhecido.", outputFormat);
            }
        }

        var resolvedCommand = command ?? PullRequestCommandKind.GetComments;

        if (resolvedCommand is not PullRequestCommandKind.GetPipelineLog
            && (latestCommitPipeline || buildNumber is not null))
        {
            return Failure("--latest-commit-pipeline e --build sao suportados apenas por bb-get-pr-pipeline-log.", outputFormat);
        }

        if (latestCommitPipeline && buildNumber is not null)
        {
            return Failure("Informe apenas uma estrategia de pipeline: --latest-commit-pipeline ou --build.", outputFormat);
        }

        return new CommandLineParseResult(
            IsHelp: false,
            HelpCommand: null,
            Options: new PullRequestCommandOptions(
                resolvedCommand,
                repository,
                pullRequest,
                url,
                includeSystem,
                latestCommitPipeline,
                buildNumber,
                outputFormat),
            Error: null,
            OutputFormat: outputFormat);
    }

    private static bool TryParseCommand(string[] args, out PullRequestCommandKind command, out int nextIndex)
    {
        command = PullRequestCommandKind.GetComments;
        nextIndex = 0;

        if (IsCommand(args[0], "bb-get-pr-comments"))
        {
            command = PullRequestCommandKind.GetComments;
            nextIndex = 1;
            return true;
        }

        if (IsCommand(args[0], "bb-get-pr-tasks"))
        {
            command = PullRequestCommandKind.GetTasks;
            nextIndex = 1;
            return true;
        }

        if (IsCommand(args[0], "bb-get-pr-reports"))
        {
            command = PullRequestCommandKind.GetReports;
            nextIndex = 1;
            return true;
        }

        if (IsCommand(args[0], "bb-get-pr-branches"))
        {
            command = PullRequestCommandKind.GetBranches;
            nextIndex = 1;
            return true;
        }

        if (IsCommand(args[0], "bb-get-pr-pipeline-log"))
        {
            command = PullRequestCommandKind.GetPipelineLog;
            nextIndex = 1;
            return true;
        }

        if (args.Length < 2 || !IsCommand(args[0], "bb"))
        {
            return false;
        }

        if (IsCommand(args[1], "get-pr-comments"))
        {
            command = PullRequestCommandKind.GetComments;
            nextIndex = 2;
            return true;
        }

        if (IsCommand(args[1], "get-pr-tasks"))
        {
            command = PullRequestCommandKind.GetTasks;
            nextIndex = 2;
            return true;
        }

        if (IsCommand(args[1], "get-pr-reports"))
        {
            command = PullRequestCommandKind.GetReports;
            nextIndex = 2;
            return true;
        }

        if (IsCommand(args[1], "get-pr-branches"))
        {
            command = PullRequestCommandKind.GetBranches;
            nextIndex = 2;
            return true;
        }

        if (IsCommand(args[1], "get-pr-pipeline-log"))
        {
            command = PullRequestCommandKind.GetPipelineLog;
            nextIndex = 2;
            return true;
        }

        return false;
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string option,
        OutputFormat outputFormat,
        out string? value,
        out CommandLineParseResult error)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = null;
            error = Failure($"Parametro '{option}' exige um valor.", outputFormat);
            return false;
        }

        index++;
        value = args[index];
        error = default!;
        return true;
    }

    private static bool TryParseOutput(string? value, out OutputFormat outputFormat)
    {
        outputFormat = OutputFormat.Table;

        if (string.Equals(value, "table", StringComparison.OrdinalIgnoreCase))
        {
            outputFormat = OutputFormat.Table;
            return true;
        }

        if (string.Equals(value, "json", StringComparison.OrdinalIgnoreCase))
        {
            outputFormat = OutputFormat.Json;
            return true;
        }

        return false;
    }

    private static bool IsCommand(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static CommandLineParseResult Help(PullRequestCommandKind? command, OutputFormat outputFormat = OutputFormat.Table)
    {
        return new CommandLineParseResult(true, command, null, null, outputFormat);
    }

    private static CommandLineParseResult Failure(string message, OutputFormat outputFormat)
    {
        return new CommandLineParseResult(
            IsHelp: false,
            HelpCommand: null,
            Options: null,
            Error: new CliError(ErrorCodes.ValidationError, message),
            OutputFormat: outputFormat);
    }
}
