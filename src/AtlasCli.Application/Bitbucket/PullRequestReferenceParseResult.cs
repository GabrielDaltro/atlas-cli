namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReferenceParseResult(
    PullRequestReference? Reference,
    string? ErrorMessage)
{
    public bool IsSuccess => Reference is not null;

    public static PullRequestReferenceParseResult Success(PullRequestReference reference)
    {
        return new PullRequestReferenceParseResult(reference, null);
    }

    public static PullRequestReferenceParseResult Failure(string errorMessage)
    {
        return new PullRequestReferenceParseResult(null, errorMessage);
    }
}
