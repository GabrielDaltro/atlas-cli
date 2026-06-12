using System.Text.Json;
using AtlasCli.Application.Sonar;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class UncoveredLineOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        IReadOnlyList<PullRequestUncoveredLine> uncoveredLines,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(uncoveredLines, writer);
            return;
        }

        await WriteTableAsync(uncoveredLines, writer);
    }

    private static async Task WriteJsonAsync(IReadOnlyList<PullRequestUncoveredLine> uncoveredLines, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            count = uncoveredLines.Count,
            data = uncoveredLines
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(IReadOnlyList<PullRequestUncoveredLine> uncoveredLines, TextWriter writer)
    {
        await writer.WriteLineAsync("FILE\tLINE\tSTATUS\tCODE");

        foreach (var line in uncoveredLines)
        {
            await writer.WriteLineAsync(
                $"{SingleLine(line.File)}\t{line.Line}\t{line.Status}\t{SingleLine(line.Code)}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
