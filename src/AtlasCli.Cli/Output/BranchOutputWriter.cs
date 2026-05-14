using System.Text.Json;
using AtlasCli.Application.Bitbucket;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class BranchOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        PullRequestBranches branches,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(branches, writer);
            return;
        }

        await WriteTableAsync(branches, writer);
    }

    private static async Task WriteJsonAsync(PullRequestBranches branches, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            data = branches
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(PullRequestBranches branches, TextWriter writer)
    {
        await writer.WriteLineAsync("SOURCE\tTARGET");
        await writer.WriteLineAsync($"{SingleLine(branches.Source)}\t{SingleLine(branches.Target)}");
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
