using System.Text.Json;
using AtlasCli.Application.Bitbucket;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class ReportOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        IReadOnlyList<PullRequestReport> reports,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(reports, writer);
            return;
        }

        await WriteTableAsync(reports, writer);
    }

    private static async Task WriteJsonAsync(IReadOnlyList<PullRequestReport> reports, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            count = reports.Count,
            data = reports
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(IReadOnlyList<PullRequestReport> reports, TextWriter writer)
    {
        await writer.WriteLineAsync("ID\tTITLE\tREPORTER\tRESULT\tDETAILS");

        foreach (var report in reports)
        {
            await writer.WriteLineAsync(
                $"{report.Id}\t{SingleLine(report.Title)}\t{SingleLine(report.Reporter)}\t{report.Result}\t{SingleLine(report.Details ?? string.Empty)}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
