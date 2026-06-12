using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AtlasCli.Application.Sonar;

namespace AtlasCli.Infrastructure.Sonar;

public sealed partial class SonarClient : ISonarCoverageGateway
{
    private const int PageSize = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string? _token;

    public SonarClient(HttpClient httpClient, string? token)
    {
        _httpClient = httpClient;
        _token = token;
    }

    public async Task<IReadOnlyList<PullRequestUncoveredLine>> GetNewUncoveredLinesAsync(
        SonarPullRequestCoverageReference reference,
        CancellationToken cancellationToken = default)
    {
        var files = await GetFilesWithNewUncoveredLinesAsync(reference, cancellationToken);
        var uncoveredLines = new List<PullRequestUncoveredLine>();

        foreach (var file in files)
        {
            var sourceLines = await GetSourceLinesAsync(reference, file.Key, cancellationToken);

            foreach (var line in sourceLines.Where(line => line.IsNew && line.LineHits == 0))
            {
                uncoveredLines.Add(new PullRequestUncoveredLine(
                    file.Path,
                    line.Line,
                    "uncovered",
                    CleanCode(line.Code)));
            }
        }

        return uncoveredLines;
    }

    private async Task<IReadOnlyList<SonarCoverageFile>> GetFilesWithNewUncoveredLinesAsync(
        SonarPullRequestCoverageReference reference,
        CancellationToken cancellationToken)
    {
        var files = new List<SonarCoverageFile>();
        var page = 1;
        var total = 0;

        do
        {
            var response = await SendJsonAsync<SonarComponentTreeResponse>(
                BuildComponentTreeUrl(reference, page),
                "arquivos com cobertura do PR",
                cancellationToken);

            total = response.Paging?.Total ?? response.Components.Count;
            files.AddRange(response.Components
                .Where(component => GetMetricValue(component.Measures, "new_uncovered_lines") > 0)
                .Select(component => new SonarCoverageFile(
                    component.Key ?? string.Empty,
                    component.Path ?? component.Name ?? component.Key ?? string.Empty))
                .Where(file => !string.IsNullOrWhiteSpace(file.Key)));

            page++;
        }
        while ((page - 1) * PageSize < total);

        return files;
    }

    private async Task<IReadOnlyList<SonarSourceLine>> GetSourceLinesAsync(
        SonarPullRequestCoverageReference reference,
        string fileKey,
        CancellationToken cancellationToken)
    {
        var response = await SendJsonAsync<SonarSourceLinesResponse>(
            BuildSourceLinesUrl(reference, fileKey),
            "linhas de codigo com cobertura do PR",
            cancellationToken);

        return response.Sources;
    }

    private async Task<T> SendJsonAsync<T>(
        string url,
        string resourceName,
        CancellationToken cancellationToken)
        where T : new()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SonarApiException(
                response.StatusCode,
                BuildErrorMessage(resourceName, response, details));
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(responseStream, JsonOptions, cancellationToken)
            ?? new T();
    }

    private string BuildComponentTreeUrl(SonarPullRequestCoverageReference reference, int page)
    {
        var query = BuildQuery(
            ("component", reference.ProjectKey),
            ("pullRequest", reference.PullRequest),
            ("metricKeys", "new_uncovered_lines,new_lines_to_cover"),
            ("qualifiers", "FIL"),
            ("strategy", "leaves"),
            ("ps", PageSize.ToString(CultureInfo.InvariantCulture)),
            ("p", page.ToString(CultureInfo.InvariantCulture)));

        return new Uri(NormalizeBaseUrl(reference.BaseUrl), $"api/measures/component_tree?{query}").ToString();
    }

    private string BuildSourceLinesUrl(SonarPullRequestCoverageReference reference, string fileKey)
    {
        var query = BuildQuery(
            ("key", fileKey),
            ("pullRequest", reference.PullRequest));

        return new Uri(NormalizeBaseUrl(reference.BaseUrl), $"api/sources/lines?{query}").ToString();
    }

    private string BuildErrorMessage(string resourceName, HttpResponseMessage response, string details)
    {
        var message = $"Sonar retornou {(int)response.StatusCode} {response.ReasonPhrase} ao ler {resourceName}.";

        if ((response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            && string.IsNullOrWhiteSpace(_token))
        {
            message = $"{message} Configure SONAR_TOKEN para autenticar no Sonar.";
        }

        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        var sanitizedDetails = details.ReplaceLineEndings(" ").Trim();
        return $"{message} {sanitizedDetails}";
    }

    private static int GetMetricValue(IReadOnlyList<SonarMeasure> measures, string metric)
    {
        var measure = measures.FirstOrDefault(value => string.Equals(value.Metric, metric, StringComparison.OrdinalIgnoreCase));
        var rawValue = measure?.Periods.FirstOrDefault()?.Value ?? measure?.Value;

        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static string CleanCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(code, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static string BuildQuery(params (string Key, string Value)[] parameters)
    {
        return string.Join(
            "&",
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }

    private static Uri NormalizeBaseUrl(string baseUrl)
    {
        return new Uri($"{baseUrl.Trim().TrimEnd('/')}/");
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    private sealed record SonarCoverageFile(string Key, string Path);

    private sealed class SonarComponentTreeResponse
    {
        [JsonPropertyName("paging")]
        public SonarPaging? Paging { get; init; }

        [JsonPropertyName("components")]
        public IReadOnlyList<SonarComponent> Components { get; init; } = [];
    }

    private sealed class SonarPaging
    {
        [JsonPropertyName("total")]
        public int Total { get; init; }
    }

    private sealed class SonarComponent
    {
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("longName")]
        public string? LongName { get; init; }

        [JsonPropertyName("measures")]
        public IReadOnlyList<SonarMeasure> Measures { get; init; } = [];
    }

    private sealed class SonarMeasure
    {
        [JsonPropertyName("metric")]
        public string? Metric { get; init; }

        [JsonPropertyName("value")]
        public string? Value { get; init; }

        [JsonPropertyName("periods")]
        public IReadOnlyList<SonarPeriod> Periods { get; init; } = [];
    }

    private sealed class SonarPeriod
    {
        [JsonPropertyName("value")]
        public string? Value { get; init; }
    }

    private sealed class SonarSourceLinesResponse
    {
        [JsonPropertyName("sources")]
        public IReadOnlyList<SonarSourceLine> Sources { get; init; } = [];
    }

    private sealed class SonarSourceLine
    {
        [JsonPropertyName("line")]
        public int Line { get; init; }

        [JsonPropertyName("code")]
        public string? Code { get; init; }

        [JsonPropertyName("lineHits")]
        public int? LineHits { get; init; }

        [JsonPropertyName("isNew")]
        public bool IsNew { get; init; }
    }

}
