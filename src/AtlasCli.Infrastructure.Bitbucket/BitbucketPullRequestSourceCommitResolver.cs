using System.Net;
using AtlasCli.Application.Bitbucket;

namespace AtlasCli.Infrastructure.Bitbucket;

public sealed class BitbucketPullRequestSourceCommitResolver : IPullRequestSourceCommitResolver
{
    private readonly IBitbucketPullRequestGateway _primaryGateway;
    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly string? _baseUrl;

    public BitbucketPullRequestSourceCommitResolver(
        IBitbucketPullRequestGateway primaryGateway,
        Func<string, string?> getEnvironmentVariable,
        string? baseUrl)
    {
        _primaryGateway = primaryGateway;
        _getEnvironmentVariable = getEnvironmentVariable;
        _baseUrl = baseUrl;
    }

    public async Task<string> GetSourceCommitAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryGateway.GetPullRequestSourceCommitAsync(pullRequest, cancellationToken);
        }
        catch (BitbucketApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            if (!BitbucketCredentials.TryFromEnvironment(
                _getEnvironmentVariable,
                pullRequest.Workspace,
                "GET_PR_COMMENTS_TOKEN",
                out var metadataCredentials,
                out _))
            {
                throw;
            }

            using var metadataHttpClient = new HttpClient();
            var metadataClient = new BitbucketClient(metadataHttpClient, _baseUrl, metadataCredentials!);
            return await metadataClient.GetPullRequestSourceCommitAsync(pullRequest, cancellationToken);
        }
    }
}
