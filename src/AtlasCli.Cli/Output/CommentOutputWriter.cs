using System.Text.Json;
using AtlasCli.Application.Bitbucket;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class CommentOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        IReadOnlyList<PullRequestComment> comments,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(comments, writer);
            return;
        }

        await WriteTableAsync(comments, writer);
    }

    private static async Task WriteJsonAsync(IReadOnlyList<PullRequestComment> comments, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            count = comments.Count,
            data = comments
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(IReadOnlyList<PullRequestComment> comments, TextWriter writer)
    {
        await writer.WriteLineAsync("ID\tAUTHOR\tDATE\tTYPE\tCONTEXT\tSTATE\tCOMMENT");

        foreach (var comment in comments)
        {
            var context = comment.File is null
                ? "-"
                : comment.Line is null
                    ? comment.File
                    : $"{comment.File}:{comment.Line}";

            await writer.WriteLineAsync(
                $"{comment.Id}\t{SingleLine(comment.Author)}\t{comment.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}\t{comment.Type}\t{context}\t{comment.State}\t{SingleLine(comment.Text)}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
