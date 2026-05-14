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
        var pullRequestDetails = await GetPullRequestAsync(pullRequest, cancellationToken);

        if (string.IsNullOrWhiteSpace(pullRequestDetails.Source?.Commit?.Hash))
        {
            throw new BitbucketApiException(
                System.Net.HttpStatusCode.NotFound,
                $"Bitbucket nao retornou o commit de origem do PR {pullRequest.Workspace}/{pullRequest.Repository}#{pullRequest.Number}.");
        }

        return pullRequestDetails.Source.Commit.Hash;
    }

    public async Task<PullRequestBranches> GetPullRequestBranchesAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var pullRequestDetails = await GetPullRequestAsync(pullRequest, cancellationToken);

        if (string.IsNullOrWhiteSpace(pullRequestDetails.Source?.Branch?.Name)
            || string.IsNullOrWhiteSpace(pullRequestDetails.Destination?.Branch?.Name))
        {
            throw new BitbucketApiException(
                System.Net.HttpStatusCode.NotFound,
                $"Bitbucket nao retornou as branches de origem e destino do PR {pullRequest.Workspace}/{pullRequest.Repository}#{pullRequest.Number}.");
        }

        return new PullRequestBranches(
            pullRequestDetails.Source.Branch.Name,
            pullRequestDetails.Destination.Branch.Name);
    }

    public async Task<int> GetReferencedPipelineBuildNumberAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken = default)
    {
        var nextUrl = BuildPullRequestStatusesUrl(pullRequest);
        BitbucketPullRequestStatus? latestPipelineStatus = null;

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
                    BuildErrorMessage(pullRequest, "statuses do PR", response, details));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<BitbucketPullRequestStatusesPage>(responseStream, JsonOptions, cancellationToken)
                ?? new BitbucketPullRequestStatusesPage();

            var pageLatestStatus = page.Values
                .Where(status => TryExtractPipelineBuildNumber(status.Url, out _))
                .OrderByDescending(status => status.UpdatedOn ?? status.CreatedOn ?? DateTimeOffset.MinValue)
                .FirstOrDefault();

            if (pageLatestStatus is not null
                && (latestPipelineStatus is null
                    || (pageLatestStatus.UpdatedOn ?? pageLatestStatus.CreatedOn ?? DateTimeOffset.MinValue)
                    > (latestPipelineStatus.UpdatedOn ?? latestPipelineStatus.CreatedOn ?? DateTimeOffset.MinValue)))
            {
                latestPipelineStatus = pageLatestStatus;
            }

            nextUrl = page.Next;
        }

        if (latestPipelineStatus is null
            || !TryExtractPipelineBuildNumber(latestPipelineStatus.Url, out var buildNumber))
        {
            throw new BitbucketApiException(
                System.Net.HttpStatusCode.NotFound,
                $"Bitbucket nao retornou um pipeline associado ao PR {pullRequest.Workspace}/{pullRequest.Repository}#{pullRequest.Number}.");
        }

        return buildNumber;
    }

    public async Task<PullRequestPipeline> GetLatestPipelineAsync(
        RepositoryReference repository,
        string commitHash,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPipelinesByCommitUrl(repository, commitHash);

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
                BuildErrorMessage(repository, "pipelines", response, details));
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var page = await JsonSerializer.DeserializeAsync<BitbucketPipelinesPage>(responseStream, JsonOptions, cancellationToken)
            ?? new BitbucketPipelinesPage();
        var pipeline = page.Values.FirstOrDefault();

        if (pipeline is null || string.IsNullOrWhiteSpace(pipeline.Uuid))
        {
            throw new BitbucketApiException(
                System.Net.HttpStatusCode.NotFound,
                $"Bitbucket nao retornou pipelines para o commit {commitHash} do repositorio {repository.Workspace}/{repository.Repository}.");
        }

        return new PullRequestPipeline(
            pipeline.Uuid,
            pipeline.BuildNumber,
            ResolveStateName(pipeline.State),
            pipeline.CreatedOn,
            pipeline.CompletedOn);
    }

    public async Task<PullRequestPipeline> GetPipelineByBuildNumberAsync(
        RepositoryReference repository,
        int buildNumber,
        CancellationToken cancellationToken = default)
    {
        var nextUrl = BuildPipelinesUrl(repository);

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
                    BuildErrorMessage(repository, "pipelines", response, details));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<BitbucketPipelinesPage>(responseStream, JsonOptions, cancellationToken)
                ?? new BitbucketPipelinesPage();

            var pipeline = page.Values.FirstOrDefault(value => value.BuildNumber == buildNumber && !string.IsNullOrWhiteSpace(value.Uuid));
            if (pipeline is not null)
            {
                return new PullRequestPipeline(
                    pipeline.Uuid!,
                    pipeline.BuildNumber,
                    ResolveStateName(pipeline.State),
                    pipeline.CreatedOn,
                    pipeline.CompletedOn);
            }

            nextUrl = page.Next;
        }

        throw new BitbucketApiException(
            System.Net.HttpStatusCode.NotFound,
            $"Bitbucket nao retornou o build {buildNumber} do repositorio {repository.Workspace}/{repository.Repository}.");
    }

    public async Task<IReadOnlyList<PullRequestPipelineStep>> GetPipelineStepsAsync(
        RepositoryReference repository,
        string pipelineUuid,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPipelineStepsUrl(repository, pipelineUuid);

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
                BuildErrorMessage(repository, "steps do pipeline", response, details));
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var page = await JsonSerializer.DeserializeAsync<BitbucketPipelineStepsPage>(responseStream, JsonOptions, cancellationToken)
            ?? new BitbucketPipelineStepsPage();

        return page.Values
            .Where(step => !string.IsNullOrWhiteSpace(step.Uuid))
            .Select(step => new PullRequestPipelineStep(
                step.Uuid!,
                FirstConfigured(step.Name, step.SetupCommands.FirstOrDefault()?.Name, step.ScriptCommands.FirstOrDefault()?.Name) ?? "step",
                ResolveStateName(step.State)))
            .ToArray();
    }

    public async Task<string> GetPipelineStepLogAsync(
        RepositoryReference repository,
        string pipelineUuid,
        string stepUuid,
        CancellationToken cancellationToken = default)
    {
        var url = BuildPipelineStepLogUrl(repository, pipelineUuid, stepUuid);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            _credentials.ToBasicAuthenticationParameter());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                return string.Empty;
            }

            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BitbucketApiException(
                response.StatusCode,
                BuildErrorMessage(repository, "log do step do pipeline", response, details));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
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

    private async Task<BitbucketPullRequest> GetPullRequestAsync(
        PullRequestReference pullRequest,
        CancellationToken cancellationToken)
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
        return await JsonSerializer.DeserializeAsync<BitbucketPullRequest>(responseStream, JsonOptions, cancellationToken)
            ?? new BitbucketPullRequest();
    }

    private string BuildCommentsUrl(PullRequestReference pullRequest)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var relativeUrl = $"repositories/{workspace}/{repository}/pullrequests/{pullRequest.Number}/comments?pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildPullRequestStatusesUrl(PullRequestReference pullRequest)
    {
        var workspace = Uri.EscapeDataString(pullRequest.Workspace);
        var repository = Uri.EscapeDataString(pullRequest.Repository);
        var relativeUrl = $"repositories/{workspace}/{repository}/pullrequests/{pullRequest.Number}/statuses?pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildPipelinesByCommitUrl(RepositoryReference repository, string commitHash)
    {
        var workspace = Uri.EscapeDataString(repository.Workspace);
        var repositoryName = Uri.EscapeDataString(repository.Repository);
        var query = Uri.EscapeDataString($"target.commit.hash=\"{commitHash}\"");
        var relativeUrl = $"repositories/{workspace}/{repositoryName}/pipelines/?sort=-created_on&q={query}";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildPipelinesUrl(RepositoryReference repository)
    {
        var workspace = Uri.EscapeDataString(repository.Workspace);
        var repositoryName = Uri.EscapeDataString(repository.Repository);
        var relativeUrl = $"repositories/{workspace}/{repositoryName}/pipelines/?sort=-created_on&pagelen=100";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildPipelineStepsUrl(RepositoryReference repository, string pipelineUuid)
    {
        var workspace = Uri.EscapeDataString(repository.Workspace);
        var repositoryName = Uri.EscapeDataString(repository.Repository);
        var pipeline = Uri.EscapeDataString(pipelineUuid);
        var relativeUrl = $"repositories/{workspace}/{repositoryName}/pipelines/{pipeline}/steps";
        return new Uri(_apiBaseUrl, relativeUrl).ToString();
    }

    private string BuildPipelineStepLogUrl(RepositoryReference repository, string pipelineUuid, string stepUuid)
    {
        var workspace = Uri.EscapeDataString(repository.Workspace);
        var repositoryName = Uri.EscapeDataString(repository.Repository);
        var pipeline = Uri.EscapeDataString(pipelineUuid);
        var step = Uri.EscapeDataString(stepUuid);
        var relativeUrl = $"repositories/{workspace}/{repositoryName}/pipelines/{pipeline}/steps/{step}/log";
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

    private static string ResolveStateName(BitbucketPipelineState? state)
    {
        return FirstConfigured(state?.Result?.Name, state?.Name, state?.Type) ?? "unknown";
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

    private static string BuildErrorMessage(
        RepositoryReference repository,
        string resourceName,
        HttpResponseMessage response,
        string details)
    {
        var message = $"Bitbucket retornou {(int)response.StatusCode} {response.ReasonPhrase} ao ler {resourceName} do repositorio {repository.Workspace}/{repository.Repository}.";

        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        var sanitizedDetails = details.ReplaceLineEndings(" ").Trim();
        return $"{message} {sanitizedDetails}";
    }

    private static bool TryExtractPipelineBuildNumber(string? url, out int buildNumber)
    {
        buildNumber = default;

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var resultsIndex = Array.FindIndex(
            segments,
            segment => string.Equals(segment, "results", StringComparison.OrdinalIgnoreCase));

        if (resultsIndex < 1
            || resultsIndex + 1 >= segments.Length
            || !string.Equals(segments[resultsIndex - 1], "pipelines", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(segments[resultsIndex + 1], out buildNumber) && buildNumber > 0;
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

    private sealed class BitbucketPipelinesPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketPipeline> Values { get; init; } = [];
    }

    private sealed class BitbucketPullRequestStatusesPage
    {
        [JsonPropertyName("next")]
        public string? Next { get; init; }

        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketPullRequestStatus> Values { get; init; } = [];
    }

    private sealed class BitbucketPipelineStepsPage
    {
        [JsonPropertyName("values")]
        public IReadOnlyList<BitbucketPipelineStep> Values { get; init; } = [];
    }

    private sealed class BitbucketPullRequest
    {
        [JsonPropertyName("source")]
        public BitbucketPullRequestSource? Source { get; init; }

        [JsonPropertyName("destination")]
        public BitbucketPullRequestDestination? Destination { get; init; }
    }

    private sealed class BitbucketPullRequestSource
    {
        [JsonPropertyName("commit")]
        public BitbucketPullRequestCommit? Commit { get; init; }

        [JsonPropertyName("branch")]
        public BitbucketPullRequestBranch? Branch { get; init; }
    }

    private sealed class BitbucketPullRequestDestination
    {
        [JsonPropertyName("branch")]
        public BitbucketPullRequestBranch? Branch { get; init; }
    }

    private sealed class BitbucketPullRequestBranch
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class BitbucketPullRequestCommit
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; init; }
    }

    private sealed class BitbucketPipeline
    {
        [JsonPropertyName("uuid")]
        public string? Uuid { get; init; }

        [JsonPropertyName("build_number")]
        public int? BuildNumber { get; init; }

        [JsonPropertyName("created_on")]
        public DateTimeOffset? CreatedOn { get; init; }

        [JsonPropertyName("completed_on")]
        public DateTimeOffset? CompletedOn { get; init; }

        [JsonPropertyName("state")]
        public BitbucketPipelineState? State { get; init; }
    }

    private sealed class BitbucketPipelineStep
    {
        [JsonPropertyName("uuid")]
        public string? Uuid { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("state")]
        public BitbucketPipelineState? State { get; init; }

        [JsonPropertyName("setup_commands")]
        public IReadOnlyList<BitbucketPipelineCommand> SetupCommands { get; init; } = [];

        [JsonPropertyName("script_commands")]
        public IReadOnlyList<BitbucketPipelineCommand> ScriptCommands { get; init; } = [];
    }

    private sealed class BitbucketPipelineCommand
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class BitbucketPipelineState
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("result")]
        public BitbucketPipelineStateResult? Result { get; init; }
    }

    private sealed class BitbucketPipelineStateResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class BitbucketPullRequestStatus
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("created_on")]
        public DateTimeOffset? CreatedOn { get; init; }

        [JsonPropertyName("updated_on")]
        public DateTimeOffset? UpdatedOn { get; init; }
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
