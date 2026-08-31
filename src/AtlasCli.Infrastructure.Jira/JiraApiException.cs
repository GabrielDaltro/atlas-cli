using System.Net;

namespace AtlasCli.Infrastructure.Jira;

public sealed class JiraApiException : Exception
{
    public JiraApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
