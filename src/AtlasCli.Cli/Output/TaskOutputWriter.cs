using System.Text.Json;
using AtlasCli.Application.Bitbucket;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class TaskOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        IReadOnlyList<PullRequestTask> tasks,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(tasks, writer);
            return;
        }

        await WriteTableAsync(tasks, writer);
    }

    private static async Task WriteJsonAsync(IReadOnlyList<PullRequestTask> tasks, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            count = tasks.Count,
            data = tasks
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(IReadOnlyList<PullRequestTask> tasks, TextWriter writer)
    {
        await writer.WriteLineAsync("ID\tCREATOR\tDATE\tSTATE\tCOMMENT_ID\tTASK");

        foreach (var task in tasks)
        {
            var commentId = task.CommentId?.ToString() ?? "-";

            await writer.WriteLineAsync(
                $"{task.Id}\t{SingleLine(task.Creator)}\t{task.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}\t{task.State}\t{commentId}\t{SingleLine(task.Text)}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
