namespace AtlasCli.Application.Bitbucket;

public sealed record RepositoryReferenceParseResult(
    RepositoryReference? Reference,
    string? ErrorMessage)
{
    public bool IsSuccess => Reference is not null;

    public static RepositoryReferenceParseResult Success(RepositoryReference reference)
    {
        return new RepositoryReferenceParseResult(reference, null);
    }

    public static RepositoryReferenceParseResult Failure(string errorMessage)
    {
        return new RepositoryReferenceParseResult(null, errorMessage);
    }
}
