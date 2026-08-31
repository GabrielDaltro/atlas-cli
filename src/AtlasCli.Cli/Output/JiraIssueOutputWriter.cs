using System.Text.Json;
using AtlasCli.Application.Jira;

namespace AtlasCli.Cli.Output;

public static class JiraIssueOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(JiraIssue issue, OutputFormat outputFormat, TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(new { ok = true, data = issue }, JsonOptions));
            return;
        }

        await writer.WriteLineAsync($"KEY: {issue.Key}");
        await writer.WriteLineAsync($"SUMMARY: {issue.Summary}");
        await writer.WriteLineAsync($"TYPE: {issue.IssueType}");
        await writer.WriteLineAsync($"STATUS: {issue.Status}");
        await writer.WriteLineAsync($"PRIORITY: {issue.Priority ?? "-"}");
        await writer.WriteLineAsync($"ASSIGNEE: {issue.Assignee?.DisplayName ?? "-"}");
        await writer.WriteLineAsync($"REPORTER: {issue.Reporter?.DisplayName ?? "-"}");
        await writer.WriteLineAsync($"LABELS: {string.Join(", ", issue.Labels)}");
        await writer.WriteLineAsync($"COMPONENTS: {string.Join(", ", issue.Components)}");
        await writer.WriteLineAsync($"URL: {issue.Url}");
        await writer.WriteLineAsync("DESCRIPTION:");
        await writer.WriteLineAsync(issue.Description);
        await writer.WriteLineAsync($"COMMENTS ({issue.Comments.Count}):");

        foreach (var comment in issue.Comments)
        {
            await writer.WriteLineAsync($"- [{comment.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}] {comment.Author?.DisplayName ?? "-"}: {SingleLine(comment.Body)}");
        }

        await writer.WriteLineAsync($"ATTACHMENTS ({issue.Attachments.Count}):");
        foreach (var attachment in issue.Attachments)
        {
            await writer.WriteLineAsync($"- {attachment.FileName} ({attachment.Size?.ToString() ?? "-"} bytes): {attachment.ContentUrl}");
        }
    }

    private static string SingleLine(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }
}
