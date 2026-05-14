namespace AtlasCli.Application.Bitbucket;

public static class PullRequestReferenceParser
{
    public static PullRequestReferenceParseResult Parse(PullRequestReferenceInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.Url) && !string.IsNullOrWhiteSpace(input.PullRequest))
        {
            return PullRequestReferenceParseResult.Failure("Informe apenas uma identificacao de PR: --url ou --pr.");
        }

        if (!string.IsNullOrWhiteSpace(input.Url))
        {
            return ParseUrl(input.Url);
        }

        if (string.IsNullOrWhiteSpace(input.PullRequest))
        {
            return PullRequestReferenceParseResult.Failure("Informe o PR usando --repo com --pr, --pr <url> ou --url <url>.");
        }

        if (LooksLikeUrl(input.PullRequest))
        {
            return ParseUrl(input.PullRequest);
        }

        if (!int.TryParse(input.PullRequest, out var pullRequestNumber) || pullRequestNumber <= 0)
        {
            return PullRequestReferenceParseResult.Failure("--pr deve ser um numero positivo ou uma URL de PR do Bitbucket.");
        }

        var repositoryParseResult = ParseRepository(input.Repository);
        if (!repositoryParseResult.IsSuccess)
        {
            return PullRequestReferenceParseResult.Failure(repositoryParseResult.ErrorMessage!);
        }

        return PullRequestReferenceParseResult.Success(new PullRequestReference(
            repositoryParseResult.Reference!.Workspace,
            repositoryParseResult.Reference.Repository,
            pullRequestNumber));
    }

    public static RepositoryReferenceParseResult ParseRepository(string? repositoryOption)
    {
        if (string.IsNullOrWhiteSpace(repositoryOption))
        {
            return RepositoryReferenceParseResult.Failure("--repo e obrigatorio quando --pr recebe apenas o numero do PR.");
        }

        var parts = repositoryOption.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return RepositoryReferenceParseResult.Failure("--repo deve estar no formato <workspace/repositorio>.");
        }

        return RepositoryReferenceParseResult.Success(new RepositoryReference(parts[0], parts[1]));
    }

    private static PullRequestReferenceParseResult ParseUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return PullRequestReferenceParseResult.Failure("A URL do PR e invalida.");
        }

        if (!string.Equals(uri.Host, "bitbucket.org", StringComparison.OrdinalIgnoreCase))
        {
            return PullRequestReferenceParseResult.Failure("A URL do PR deve ser do Bitbucket Cloud.");
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var pullRequestsIndex = Array.FindIndex(
            segments,
            segment => string.Equals(segment, "pull-requests", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "pullrequests", StringComparison.OrdinalIgnoreCase));

        if (pullRequestsIndex != 2 || segments.Length <= pullRequestsIndex + 1)
        {
            return PullRequestReferenceParseResult.Failure("A URL do PR deve seguir o formato https://bitbucket.org/<workspace>/<repositorio>/pull-requests/<numero>.");
        }

        if (!int.TryParse(segments[pullRequestsIndex + 1], out var pullRequestNumber) || pullRequestNumber <= 0)
        {
            return PullRequestReferenceParseResult.Failure("A URL do PR deve conter um numero de PR positivo.");
        }

        return PullRequestReferenceParseResult.Success(new PullRequestReference(segments[0], segments[1], pullRequestNumber));
    }

    private static bool LooksLikeUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
