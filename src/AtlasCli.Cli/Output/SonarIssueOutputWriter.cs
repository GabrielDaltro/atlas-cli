using System.Text.Json;
using AtlasCli.Application.Sonar;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class SonarIssueOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        IReadOnlyList<PullRequestSonarIssue> issues,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(issues, writer);
            return;
        }

        await WriteTableAsync(issues, writer);
    }

    private static async Task WriteJsonAsync(IReadOnlyList<PullRequestSonarIssue> issues, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            count = issues.Count,
            data = issues
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(IReadOnlyList<PullRequestSonarIssue> issues, TextWriter writer)
    {
        await writer.WriteLineAsync("FILE\tLINE\tSEVERITY\tTYPE\tKEY\tMESSAGE\tLINK");

        foreach (var issue in issues)
        {
            await writer.WriteLineAsync(
                $"{SingleLine(issue.File)}\t{issue.Line?.ToString() ?? string.Empty}\t{issue.Severity}\t{issue.Type}\t{issue.Key}\t{SingleLine(issue.Message)}\t{issue.Link}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
