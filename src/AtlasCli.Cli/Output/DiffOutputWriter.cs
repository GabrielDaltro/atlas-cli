using System.Text.Json;
using AtlasCli.Application.Bitbucket;

namespace AtlasCli.Cli.Output;

public static class DiffOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        PullRequestDiff diff,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            var payload = new
            {
                ok = true,
                data = diff
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        await writer.WriteAsync(diff.Content);
    }
}
