using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AtlasCli.Application.Bitbucket;

namespace AtlasCli.Infrastructure.Bitbucket;

public sealed class BitbucketClient : IBitbucketPullRequestGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _apiBaseUrl;
    private readonly BitbucketCredentials _credentials;

    public BitbucketClient(HttpClient httpClient, string? apiBaseUrl, BitbucketCredentials credentials)
    {
        _httpClient = httpClient;
        _apiBaseUrl = NormalizeApiBaseUrl(apiBaseUrl);
        _credentials = credentials;
    }

    public async Task<IReadOnlyList<PullRequestComment>> GetPullRequestCommentsAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var comments = new List<PullRequestComment>();
        var nextUrl = BuildCommentsUrl(pullRequest);

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                _credentials.ToBasicAuthenticationParameter());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BitbucketApiException(
                    response.StatusCode,
                    BuildErrorMessage(pullRequest, "comentarios", response, details));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<BitbucketCommentsPage>(responseStream, JsonOptions, cancellationToken)
                ?? new BitbucketCommentsPage();

            comments.AddRange(page.Values.Select(MapComment));
            nextUrl = page.Next;
        }

        return comments;
    }

    public async Task<IReadOnlyList<PullRequestTask>> GetPullRequestTasksAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<PullRequestTask>();
        var nextUrl = BuildTasksUrl(pullRequest);

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                _credentials.ToBasicAuthenticationParameter());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BitbucketApiException(
                    response.StatusCode,
                    BuildErrorMessage(pullRequest, "tasks", response, details));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<BitbucketTasksPage>(responseStream, JsonOptions, cancellationToken)
                ?? new BitbucketTasksPage();

            tasks.AddRange(page.Values.Select(MapTask));
            nextUrl = page.Next;
        }

        return tasks;
    }

    public async Task<string> GetPullRequestSourceCommitAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPullRequestUrl(pullRequest);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            _credentials.ToBasicAuthenticationParameter());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BitbucketApiException(
                response.StatusCode,
                BuildErrorMessage(pullRequest, "metadados", response, details));
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var pullRequestDetails = await JsonSerializer.DeserializeAsync<BitbucketPullRequest>(responseStream, JsonOptions, cancellationToken)
            ?? new BitbucketPullRequest();

        if (string.IsNullOrWhiteSpace(pullRequestDetails.Source?.Commit?.Hash))
        {
            throw new BitbucketApiException(
                System.Net.HttpStatusCode.NotFound,
                $"Bitbucket nao retornou o commit de origem do PR {pullRequest.Workspace}/{pullRequest.Repository}#{pullRequest.Number}.");
        }

        return pullRequestDetails.Source.Commit.Hash;
    }

    public async Task<IReadOnlyList<PullRequestReport>> GetCommitReportsAsync(
        PullRequestReference pullRequest,
        string commitHash,
        CancellationToken cancellationToken = default)
    {
        var reports = new List<PullRequestReport>();
        var nextUrl = BuildReportsUrl(pullRequest, commitHash);

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                _credentials.ToBasicAuthenticationParameter());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BitbucketApiException(
                    response.StatusCode,
                    BuildErrorMessage(pullRequest, "reports", response, details));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<BitbucketReportsPage>(responseStream, JsonOptions, cancellationToken)
                ?? new BitbucketReportsPage();

            reports.AddRange(page.Values.Select(MapReport));
            nextUrl = page.Next;
        }

        return reports;
    }

    private string BuildPullRequestUrl(PullRequestReference pullRequest)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var relativeUrl = $"repositories/{workspace}/{repository}/pullrequests/{pullRequest.Number}";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildCommentsUrl(PullRequestReference pullRequest)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var relativeUrl = $"repositories/{workspace}/{repository}/pullrequests/{pullRequest.Number}/comments?pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildTasksUrl(PullRequestReference pullRequest)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var relativeUrl = $"repositories/{workspace}/{repository}/pullrequests/{pullRequest.Number}/tasks?pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildReportsUrl(PullRequestReference pullRequest, string commitHash)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var commit = Uri.EscapeDataString(commitHash);
        var relativeUrl = $"repositories/{workspace}/{repository}/commit/{commit}/reports?pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private static PullRequestComment MapComment(BitbucketComment comment)
    {
        var inlineLine = comment.Inline?.To
            ?? comment.Inline?.From
            ?? comment.Inline?.StartTo
            ?? comment.Inline?.StartFrom;

        var type = comment.Parent?.Id is not null
            ? "reply"
            : comment.Inline is not null
                ? "inline"
                : "global";

        var state = comment.Deleted
            ? "deleted"
            : comment.Pending
                ? "pending"
                : comment.Resolution is not null
                    ? "resolved"
                    : "open";

        return new PullRequestComment(
            comment.Id,
            ResolveAuthor(comment.User),
            comment.CreatedOn,
            comment.UpdatedOn,
            comment.Content?.Raw ?? string.Empty,
            type,
            comment.Inline?.Path,
            inlineLine,
            state,
            comment.Links?.Html?.Href,
            comment.Parent?.Id);
    }

    private static PullRequestTask MapTask(BitbucketTask task)
    {
        return new PullRequestTask(
            task.Id,
            ResolveAuthor(task.Creator),
            task.CreatedOn,
            task.UpdatedOn,
            task.Content?.Raw ?? string.Empty,
            task.State ?? "unknown",
            task.Pending,
            task.ResolvedOn,
            task.ResolvedBy is null ? null : ResolveAuthor(task.ResolvedBy),
            task.Comment?.Id,
            task.Links?.Html?.Href);
    }

    private static PullRequestReport MapReport(BitbucketReport report)
    {
        return new PullRequestReport(
            report.Uuid ?? string.Empty,
            report.Title ?? string.Empty,
            report.Reporter ?? string.Empty,
            report.ReportType ?? string.Empty,
            report.Result ?? string.Empty,
            report.Link,
            report.Details,
            report.Data.Select(data => new PullRequestReportData(
                data.Title ?? string.Empty,
                data.Type ?? string.Empty,
                data.Value)).ToArray());
    }

    private static string ResolveAuthor(BitbucketUser? user)
    {
        return FirstConfigured(user?.DisplayName, user?.Nickname, user?.AccountId, user?.Uuid) ?? "desconhecido";
    }

    private static string? FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string BuildErrorMessage(
        PullRequestReference pullRequest,
        string resourceName,
        HttpResponseMessage response,
        string details)
    {
        var message = $"Bitbucket retornou {(int)response.StatusCode} {response.ReasonPhrase} ao ler {resourceName} do PR {pullRequest.Workspace}/{pullRequest.Repository}#{pullRequest.Number}.";

        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        var sanitizedDetails = details.ReplaceLineEndings(" ").Trim();
        return $"{message} {sanitizedDetails}";
    }

    private static Uri NormalizeApiBaseUrl(string? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return new Uri("https://api.bitbucket.org/2.0/");
        }

        var trimmed = apiBaseUrl.Trim().TrimEnd('/');

        if (string.Equals(trimmed, "https://bitbucket.org", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri("https://api.bitbucket.org/2.0/");
        }

        return new Uri($"{trimmed}/");
    }

    private sealed class BitbucketCommentsPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketComment> Values { get; init; } = [];
    }

    private sealed class BitbucketTasksPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketTask> Values { get; init; } = [];
    }

    private sealed class BitbucketReportsPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketReport> Values { get; init; } = [];
    }

    private sealed class BitbucketPullRequest
    {
        [JsonPropertyName("source")]
        public BitbucketPullRequestSource? Source { get; init; }
    }

    private sealed class BitbucketPullRequestSource
    {
        [JsonPropertyName("commit")]
        public BitbucketPullRequestCommit? Commit { get; init; }
    }

    private sealed class BitbucketPullRequestCommit
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; init; }
    }

    private sealed class BitbucketComment
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("created_on")]
        public DateTimeOffset? CreatedOn { get; init; }

        [JsonPropertyName("updated_on")]
        public DateTimeOffset? UpdatedOn { get; init; }

        [JsonPropertyName("content")]
        public BitbucketContent? Content { get; init; }

        [JsonPropertyName("user")]
        public BitbucketUser? User { get; init; }

        [JsonPropertyName("inline")]
        public BitbucketInline? Inline { get; init; }

        [JsonPropertyName("links")]
        public BitbucketLinks? Links { get; init; }

        [JsonPropertyName("resolution")]
        public BitbucketResolution? Resolution { get; init; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; init; }

        [JsonPropertyName("pending")]
        public bool Pending { get; init; }

        [JsonPropertyName("parent")]
        public BitbucketParentComment? Parent { get; init; }
    }

    private sealed class BitbucketContent
    {
        [JsonPropertyName("raw")]
        public string? Raw { get; init; }
    }

    private sealed class BitbucketUser
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; init; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; init; }

        [JsonPropertyName("uuid")]
        public string? Uuid { get; init; }
    }

    private sealed class BitbucketInline
    {
        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("from")]
        public int? From { get; init; }

        [JsonPropertyName("to")]
        public int? To { get; init; }

        [JsonPropertyName("start_from")]
        public int? StartFrom { get; init; }

        [JsonPropertyName("start_to")]
        public int? StartTo { get; init; }
    }

    private sealed class BitbucketLinks
    {
        [JsonPropertyName("html")]
        public BitbucketLink? Html { get; init; }
    }

    private sealed class BitbucketLink
    {
        [JsonPropertyName("href")]
        public string? Href { get; init; }
    }

    private sealed class BitbucketResolution
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }
    }

    private sealed class BitbucketParentComment
    {
        [JsonPropertyName("id")]
        public long? Id { get; init; }
    }

    private sealed class BitbucketTask
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("created_on")]
        public DateTimeOffset? CreatedOn { get; init; }

        [JsonPropertyName("updated_on")]
        public DateTimeOffset? UpdatedOn { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("content")]
        public BitbucketContent? Content { get; init; }

        [JsonPropertyName("creator")]
        public BitbucketUser? Creator { get; init; }

        [JsonPropertyName("pending")]
        public bool Pending { get; init; }

        [JsonPropertyName("resolved_on")]
        public DateTimeOffset? ResolvedOn { get; init; }

        [JsonPropertyName("resolved_by")]
        public BitbucketUser? ResolvedBy { get; init; }

        [JsonPropertyName("links")]
        public BitbucketLinks? Links { get; init; }

        [JsonPropertyName("comment")]
        public BitbucketTaskComment? Comment { get; init; }
    }

    private sealed class BitbucketTaskComment
    {
        [JsonPropertyName("id")]
        public long? Id { get; init; }
    }

    private sealed class BitbucketReport
    {
        [JsonPropertyName("uuid")]
        public string? Uuid { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("reporter")]
        public string? Reporter { get; init; }

        [JsonPropertyName("report_type")]
        public string? ReportType { get; init; }

        [JsonPropertyName("result")]
        public string? Result { get; init; }

        [JsonPropertyName("link")]
        public string? Link { get; init; }

        [JsonPropertyName("details")]
        public string? Details { get; init; }

        [JsonPropertyName("data")]
        public IReadOnlyList<BitbucketReportData> Data { get; init; } = [];
    }

    private sealed class BitbucketReportData
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("value")]
        public JsonElement? Value { get; init; }
    }
}
