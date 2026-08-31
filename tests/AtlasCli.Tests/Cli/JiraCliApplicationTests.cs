using System.Net;
using System.Text;
using System.Text.Json;
using AtlasCli.Cli;

namespace AtlasCli.Tests.Cli;

public sealed class JiraCliApplicationTests
{
    [Fact]
    public async Task ShouldReturnIssueContentWhenJiraCardIsRequested()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["JIRA_BASE_URL"] = "https://company.atlassian.net",
            ["JIRA_EMAIL"] = "developer@example.com",
            ["JIRA_API_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new QueueHttpMessageHandler(
            TenantInfoResponse(),
            JsonResponse(
                """
                {
                  "key": "DYN-123",
                  "fields": {
                    "summary": "Implementar leitura do Jira",
                    "description": {
                      "type": "doc",
                      "content": [
                        { "type": "paragraph", "content": [{ "type": "text", "text": "Descricao do card." }] }
                      ]
                    },
                    "issuetype": { "name": "Story" },
                    "status": { "name": "Em andamento" },
                    "priority": { "name": "Alta" },
                    "assignee": { "displayName": "Ana Silva", "accountId": "ana-id" },
                    "reporter": { "displayName": "Bruno Lima", "accountId": "bruno-id" },
                    "labels": ["backend", "cli"],
                    "components": [{ "name": "Platform" }],
                    "created": "2026-07-20T10:00:00.000-0300",
                    "updated": "2026-07-20T11:00:00.000-0300",
                    "attachment": [
                      {
                        "id": "10001",
                        "filename": "evidencia.pdf",
                        "mimeType": "application/pdf",
                        "size": 400,
                        "created": "2026-07-20T10:00:00.000-0300",
                        "author": { "displayName": "Ana Silva", "accountId": "ana-id" },
                        "content": "https://company.atlassian.net/secure/attachment/10001/evidencia.pdf"
                      }
                    ]
                  }
                }
                """),
            JsonResponse(
                """
                {
                  "startAt": 0,
                  "maxResults": 100,
                  "total": 1,
                  "comments": [
                    {
                      "id": "9001",
                      "author": { "displayName": "Carla Souza", "accountId": "carla-id" },
                      "body": {
                        "type": "doc",
                        "content": [
                          { "type": "paragraph", "content": [{ "type": "text", "text": "Comentario do card." }] }
                        ]
                      },
                      "created": "2026-07-20T12:00:00.000-0300",
                      "updated": "2026-07-20T12:00:00.000-0300"
                    }
                  ]
                }
                """));

        var exitCode = await CliApplication.RunAsync(
            ["jira-get-issue", "--issue", "DYN-123", "--output", "json"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        using var json = JsonDocument.Parse(standardOutput.ToString());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("DYN-123", data.GetProperty("key").GetString());
        Assert.Equal("Descricao do card.", data.GetProperty("description").GetString());
        Assert.Equal("Comentario do card.", data.GetProperty("comments")[0].GetProperty("body").GetString());
        Assert.Equal("evidencia.pdf", data.GetProperty("attachments")[0].GetProperty("fileName").GetString());
        Assert.Equal(3, httpHandler.Requests.Count);
        Assert.Contains("/_edge/tenant_info", httpHandler.Requests[0].RequestUri!.ToString());
        Assert.Contains("/ex/jira/cloud-id/rest/api/3/issue/DYN-123?fields=", httpHandler.Requests[1].RequestUri!.ToString());
        Assert.Contains("/ex/jira/cloud-id/rest/api/3/issue/DYN-123/comment?startAt=0&maxResults=100", httpHandler.Requests[2].RequestUri!.ToString());
        Assert.Equal("api.atlassian.com", httpHandler.Requests[1].RequestUri!.Host);
        Assert.Equal("Basic", httpHandler.Requests[1].Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task ShouldReturnAuthenticationErrorWhenJiraReturnsUnauthorized()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["JIRA_BASE_URL"] = "https://company.atlassian.net",
            ["JIRA_EMAIL"] = "developer@example.com",
            ["JIRA_API_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new QueueHttpMessageHandler(
            TenantInfoResponse(),
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{ \"errorMessages\": [\"Unauthorized\"] }", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{ \"errorMessages\": [\"Unauthorized\"] }", Encoding.UTF8, "application/json")
            });

        var exitCode = await CliApplication.RunAsync(
            ["jira-get-issue", "--issue", "DYN-123", "--output", "json"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        using var json = JsonDocument.Parse(standardError.ToString());
        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.Equal(ErrorCodes.AuthenticationOrConfigurationError, json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ShouldUseCardUrlAsBaseWhenJiraBaseUrlIsNotConfigured()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["JIRA_EMAIL"] = "developer@example.com",
            ["JIRA_API_TOKEN"] = "token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new QueueHttpMessageHandler(
            TenantInfoResponse(),
            JsonResponse("""{ "key": "DYN-123", "fields": {} }"""),
            JsonResponse("""{ "total": 0, "comments": [] }"""));

        var exitCode = await CliApplication.RunAsync(
            ["jira-get-issue", "--issue", "https://company.atlassian.net/browse/DYN-123"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.Equal("company.atlassian.net", httpHandler.Requests[0].RequestUri!.Host);
        Assert.Equal("api.atlassian.com", httpHandler.Requests[1].RequestUri!.Host);
    }

    [Fact]
    public async Task ShouldUseSiteApiWhenScopedApiProxyRejectsLegacyToken()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["JIRA_BASE_URL"] = "https://company.atlassian.net",
            ["JIRA_EMAIL"] = "developer@example.com",
            ["JIRA_API_TOKEN"] = "legacy-token"
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var httpHandler = new QueueHttpMessageHandler(
            TenantInfoResponse(),
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{ \"errorMessages\": [\"Unauthorized\"] }", Encoding.UTF8, "application/json")
            },
            JsonResponse("""{ "key": "DYN-123", "fields": {} }"""),
            JsonResponse("""{ "total": 0, "comments": [] }"""));

        var exitCode = await CliApplication.RunAsync(
            ["jira-get-issue", "--issue", "DYN-123"],
            environment,
            standardOutput,
            standardError,
            httpHandler);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.Equal("api.atlassian.com", httpHandler.Requests[1].RequestUri!.Host);
        Assert.Equal("company.atlassian.net", httpHandler.Requests[2].RequestUri!.Host);
        Assert.Equal("company.atlassian.net", httpHandler.Requests[3].RequestUri!.Host);
    }

    private static HttpResponseMessage TenantInfoResponse()
    {
        return JsonResponse("""{ "cloudId": "cloud-id" }""");
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        private readonly List<HttpRequestMessage> _requests = [];

        public QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
