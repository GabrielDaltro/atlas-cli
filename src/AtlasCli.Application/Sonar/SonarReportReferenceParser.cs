using AtlasCli.Application.Bitbucket;

namespace AtlasCli.Application.Sonar;

public static class SonarReportReferenceParser
{
    public static bool TryParse(
        IReadOnlyList<PullRequestReport> reports,
        out SonarPullRequestCoverageReference? reference)
    {
        foreach (var report in reports)
        {
            if (TryParse(report.Link, out reference))
            {
                return true;
            }
        }

        reference = null;
        return false;
    }

    private static bool TryParse(string? link, out SonarPullRequestCoverageReference? reference)
    {
        reference = null;

        if (string.IsNullOrWhiteSpace(link)
            || !Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("id", out var projectKey)
            || !query.TryGetValue("pullRequest", out var pullRequest)
            || string.IsNullOrWhiteSpace(projectKey)
            || string.IsNullOrWhiteSpace(pullRequest))
        {
            return false;
        }

        reference = new SonarPullRequestCoverageReference(
            ResolveBaseUrl(uri),
            projectKey,
            pullRequest);
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmedQuery = query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return values;
        }

        foreach (var pair in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            var rawKey = separatorIndex < 0 ? pair : pair[..separatorIndex];
            var rawValue = separatorIndex < 0 ? string.Empty : pair[(separatorIndex + 1)..];
            var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
            var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));

            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string ResolveBaseUrl(Uri uri)
    {
        var path = uri.AbsolutePath;
        var basePath = ResolveBasePath(path);
        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, basePath);
        return builder.Uri.ToString();
    }

    private static string ResolveBasePath(string path)
    {
        foreach (var segment in new[] { "/dashboard", "/project" })
        {
            var index = path.IndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return path[..index].TrimEnd('/') + "/";
            }
        }

        return "/";
    }
}
