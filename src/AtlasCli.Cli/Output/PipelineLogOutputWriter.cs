using System.Text.Json;
using AtlasCli.Application.Bitbucket;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class PipelineLogOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(
        PullRequestPipelineLog pipelineLog,
        OutputFormat outputFormat,
        TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            await WriteJsonAsync(pipelineLog, writer);
            return;
        }

        await WriteTableAsync(pipelineLog, writer);
    }

    private static async Task WriteJsonAsync(PullRequestPipelineLog pipelineLog, TextWriter writer)
    {
        var payload = new
        {
            ok = true,
            data = pipelineLog
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteTableAsync(PullRequestPipelineLog pipelineLog, TextWriter writer)
    {
        var buildNumber = pipelineLog.BuildNumber?.ToString() ?? "-";
        var createdAt = pipelineLog.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "-";
        var completedAt = pipelineLog.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "-";

        await writer.WriteLineAsync(
            $"PIPELINE\t{pipelineLog.PipelineUuid}\tBUILD\t{buildNumber}\tSTATE\t{pipelineLog.State}\tCREATED\t{createdAt}\tCOMPLETED\t{completedAt}");

        foreach (var step in pipelineLog.Steps)
        {
            await writer.WriteLineAsync(string.Empty);
            await writer.WriteLineAsync($"STEP\t{step.StepName}\t{step.StepUuid}\tSTATE\t{step.State}");
            await writer.WriteLineAsync(step.Log.TrimEnd());
        }
    }
}
