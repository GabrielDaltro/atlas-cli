namespace AtlasCli.Cli;

public static class JiraCommandLine
{
    public static bool IsJiraInvocation(string[] args)
    {
        return args.Length > 0 && (IsCommand(args[0], "jira-get-issue") || IsCommand(args[0], "jira"));
    }

    public static JiraCommandLineParseResult Parse(string[] args)
    {
        if (args.Length is 0 || args[0] is "--help" or "-h" or "help")
        {
            return Help();
        }

        var index = 0;
        if (IsCommand(args[0], "jira-get-issue"))
        {
            index = 1;
        }
        else if (args.Length >= 2 && IsCommand(args[0], "jira") && IsCommand(args[1], "get-issue"))
        {
            index = 2;
        }
        else if (IsCommand(args[0], "jira")
                 && args.Length >= 2
                 && (args[1] is "--help" or "-h" or "help"))
        {
            return Help();
        }
        else if (IsCommand(args[0], "jira"))
        {
            return Failure("Comando Jira nao reconhecido.", OutputFormat.Table);
        }
        else
        {
            return Failure($"Comando '{args[0]}' nao reconhecido.", OutputFormat.Table);
        }

        string? issue = null;
        var outputFormat = OutputFormat.Table;

        for (var i = index; i < args.Length; i++)
        {
            var argument = args[i];
            if (argument is "--help" or "-h")
            {
                return Help(outputFormat);
            }

            switch (argument)
            {
                case "--issue":
                case "--url":
                    if (!TryReadValue(args, ref i, argument, outputFormat, out issue, out var issueError))
                    {
                        return issueError;
                    }

                    break;

                case "--output":
                    if (!TryReadValue(args, ref i, argument, outputFormat, out var output, out var outputError))
                    {
                        return outputError;
                    }

                    if (!TryParseOutput(output, out outputFormat))
                    {
                        return Failure("--output deve ser 'table' ou 'json'.", outputFormat);
                    }

                    break;

                default:
                    return Failure($"Parametro '{argument}' nao reconhecido.", outputFormat);
            }
        }

        if (string.IsNullOrWhiteSpace(issue))
        {
            return Failure("Informe --issue <chave-ou-url-do-card>.", outputFormat);
        }

        return new JiraCommandLineParseResult(
            IsHelp: false,
            Options: new JiraCommandOptions(issue, outputFormat),
            Error: null,
            OutputFormat: outputFormat);
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string option,
        OutputFormat outputFormat,
        out string? value,
        out JiraCommandLineParseResult error)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = null;
            error = Failure($"Parametro '{option}' exige um valor.", outputFormat);
            return false;
        }

        value = args[++index];
        error = default!;
        return true;
    }

    private static bool TryParseOutput(string? value, out OutputFormat outputFormat)
    {
        outputFormat = OutputFormat.Table;

        if (string.Equals(value, "table", StringComparison.OrdinalIgnoreCase))
        {
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

    private static JiraCommandLineParseResult Help(OutputFormat outputFormat = OutputFormat.Table)
    {
        return new JiraCommandLineParseResult(true, null, null, outputFormat);
    }

    private static JiraCommandLineParseResult Failure(string message, OutputFormat outputFormat)
    {
        return new JiraCommandLineParseResult(
            IsHelp: false,
            Options: null,
            Error: new CliError(ErrorCodes.ValidationError, message),
            OutputFormat: outputFormat);
    }
}
