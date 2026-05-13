using System.Net;
using System.Text;
using System.Text.Json;
using AtlasCli.Cli;

namespace AtlasCli.Tests.Cli;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task ShouldPrintGeneralHelpWhenHelpOptionIsProvided()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["--help"],
            new DictionaryEnvironment(new Dictionary<string, string>()),
            standardOutput,
            standardError);

        Assert.Equal(0, exitCode);
        Assert.Contains("Comandos:", standardOutput.ToString());
        Assert.Contains("bb-get-pr-reports", standardOutput.ToString());
        Assert.Equal(string.Empty, standardError.ToString());
    }

    [Fact]
    public async Task ShouldPrintCommandHelpWhenCommandHelpIsProvided()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-reports", "--help"],
            new DictionaryEnvironment(new Dictionary<string, string>()),
            standardOutput,
            standardError);

        Assert.Equal(0, exitCode);
        Assert.Contains("BB_<WORKSPACE>_GET_PR_REPORTS_TOKEN", standardOutput.ToString());
        Assert.DoesNotContain("BB_<WORKSPACE>_GET_PR_TASKS_TOKEN", standardOutput.ToString());
        Assert.Equal(string.Empty, standardError.ToString());
    }

    [Fact]
    public async Task ShouldReturnJsonEnvelopeWhenOutputIsJson()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_WORKSPACE_EMAIL"] = "developer@example.com",
            ["BB_WORKSPACE_GET_PR_COMMENTS_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new StubHttpMessageHandler(HttpStatusCode.OK, """
            {
              "values": [
                {
                  "id": 123,
                  "created_on": "2026-05-05T12:00:00+00:00",
                  "updated_on": "2026-05-05T12:10:00+00:00",
                  "content": { "raw": "Ajustar este ponto." },
                  "user": { "display_name": "Ana Silva" },
                  "inline": { "path": "src/Foo.cs", "to": 42 },
                  "links": { "html": { "href": "https://bitbucket.org/workspace/repo/pull-requests/1/_/diff#comment-123" } }
                }
              ]
            }
            """);

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        using var json = JsonDocument.Parse(standardOutput.ToString());
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("Ana Silva", json.RootElement.GetProperty("data")[0].GetProperty("author").GetString());
        Assert.Equal("src/Foo.cs", json.RootElement.GetProperty("data")[0].GetProperty("file").GetString());
    }

    [Fact]
    public async Task ShouldReturnTaskJsonEnvelopeWhenCommandIsGetPrTasks()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_WORKSPACE_EMAIL"] = "developer@example.com",
            ["BB_WORKSPACE_GET_PR_TASKS_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new StubHttpMessageHandler(HttpStatusCode.OK, """
            {
              "values": [
                {
                  "id": 456,
                  "created_on": "2026-05-05T12:00:00+00:00",
                  "updated_on": "2026-05-05T12:10:00+00:00",
                  "state": "OPEN",
                  "content": { "raw": "Adicionar cancellation token." },
                  "creator": { "display_name": "Ana Silva" },
                  "pending": false,
                  "comment": { "id": 123 },
                  "links": { "html": { "href": "https://bitbucket.org/workspace/repo/pull-requests/1/_/diff#task-456" } }
                }
              ]
            }
            """);

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-tasks", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        using var json = JsonDocument.Parse(standardOutput.ToString());
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("Ana Silva", json.RootElement.GetProperty("data")[0].GetProperty("creator").GetString());
        Assert.Equal(123, json.RootElement.GetProperty("data")[0].GetProperty("commentId").GetInt64());
    }

    [Fact]
    public async Task ShouldReturnReportJsonEnvelopeWhenCommandIsGetPrReports()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_WORKSPACE_EMAIL"] = "developer@example.com",
            ["BB_WORKSPACE_GET_PR_REPORTS_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new QueueHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "source": {
                        "commit": {
                          "hash": "abc123"
                        }
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "values": [
                        {
                          "uuid": "{report-id}",
                          "title": "SonarQube Cloud Code Analysis",
                          "reporter": "SonarCloud",
                          "report_type": "BUG",
                          "result": "FAILED",
                          "link": "https://sonarcloud.io/dashboard?id=project&pullRequest=1",
                          "details": "Quality Gate Failed",
                          "data": [
                            {
                              "title": "Code Coverage",
                              "type": "PERCENTAGE",
                              "value": 0
                            }
                          ]
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-reports", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        using var json = JsonDocument.Parse(standardOutput.ToString());
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("SonarCloud", json.RootElement.GetProperty("data")[0].GetProperty("reporter").GetString());
        Assert.Equal("FAILED", json.RootElement.GetProperty("data")[0].GetProperty("result").GetString());
    }

    [Fact]
    public async Task ShouldReturnConfigurationErrorWhenTokenIsMissing()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_WORKSPACE_EMAIL"] = "developer@example.com"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError);

        using var json = JsonDocument.Parse(standardError.ToString());
        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(ErrorCodes.AuthenticationOrConfigurationError, json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ShouldReturnConfigurationErrorWhenOnlyGlobalCredentialsAreConfigured()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_EMAIL"] = "developer@example.com",
            ["BB_GET_PR_COMMENTS_TOKEN"] = "global-token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError);

        using var json = JsonDocument.Parse(standardError.ToString());
        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(ErrorCodes.AuthenticationOrConfigurationError, json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ShouldReturnConfigurationErrorWhenOnlyAppPasswordIsConfigured()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_USER"] = "developer@example.com",
            ["BITBUCKET_APP_PASSWORD"] = "legacy-app-password"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"],
            environment,
            standardOutput,
            standardError);

        using var json = JsonDocument.Parse(standardError.ToString());
        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(ErrorCodes.AuthenticationOrConfigurationError, json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
