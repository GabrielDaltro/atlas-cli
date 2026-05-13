using System.Text.Json;
using AtlasCli.Cli;

namespace AtlasCli.Cli.Output;

public static class ErrorOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(CliError error, OutputFormat outputFormat, TextWriter writer)
    {
        if (outputFormat is OutputFormat.Json)
        {
            var payload = new
            {
                ok = false,
                error
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        await writer.WriteLineAsync($"{error.Code}: {error.Message}");
    }
}
