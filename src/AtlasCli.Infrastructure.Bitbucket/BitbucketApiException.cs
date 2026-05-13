using System.Net;

namespace AtlasCli.Infrastructure.Bitbucket;

public sealed class BitbucketApiException : Exception
{
    public BitbucketApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
