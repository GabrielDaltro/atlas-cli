using System.Net;

namespace AtlasCli.Infrastructure.Sonar;

public sealed class SonarApiException : Exception
{
    public SonarApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
