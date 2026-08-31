using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AtlasCli.Application.Jira;

namespace AtlasCli.Infrastructure.Jira;

public sealed class JiraClient : IJiraIssueGateway
{
    private const string IssueFields = "summary,description,issuetype,status,priority,assignee,reporter,labels,components,attachment,created,updated";

    private readonly HttpClient _httpClient;
    private readonly Uri _siteBaseUrl;
    private readonly JiraCredentials _credentials;
    private Uri? _requestBaseUrl;
    private bool _usesAtlassianApiProxy;

    public JiraClient(HttpClient httpClient, Uri baseUrl, JiraCredentials credentials)
    {
        _httpClient = httpClient;
        _siteBaseUrl = NormalizeBaseUrl(baseUrl);
        _credentials = credentials;
    }

    public async Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        var issue = await GetIssueDocumentAsync(issueKey, cancellationToken);
        var comments = await GetCommentsAsync(issueKey, cancellationToken);

        var fields = issue.GetProperty("fields");
        return new JiraIssue(
            GetString(issue, "key") ?? issueKey,
            GetString(fields, "summary") ?? string.Empty,
            ToPlainText(GetPropertyOrDefault(fields, "description")),
            GetString(GetPropertyOrDefault(fields, "issuetype"), "name") ?? string.Empty,
            GetString(GetPropertyOrDefault(fields, "status"), "name") ?? string.Empty,
            GetString(GetPropertyOrDefault(fields, "priority"), "name"),
            MapUser(GetPropertyOrDefault(fields, "assignee")),
            MapUser(GetPropertyOrDefault(fields, "reporter")),
            GetStringArray(GetPropertyOrDefault(fields, "labels")),
            GetNamedArray(GetPropertyOrDefault(fields, "components")),
            GetDateTimeOffset(fields, "created"),
            GetDateTimeOffset(fields, "updated"),
            comments,
            GetAttachments(GetPropertyOrDefault(fields, "attachment")),
            new Uri(_siteBaseUrl, $"browse/{Uri.EscapeDataString(GetString(issue, "key") ?? issueKey)}").ToString());
    }

    private async Task<JsonElement> GetIssueDocumentAsync(string issueKey, CancellationToken cancellationToken)
    {
        var relativeUrl = $"rest/api/3/issue/{Uri.EscapeDataString(issueKey)}?fields={Uri.EscapeDataString(IssueFields)}";
        return await GetDocumentAsync(relativeUrl, issueKey, "card", cancellationToken);
    }

    private async Task<IReadOnlyList<JiraComment>> GetCommentsAsync(string issueKey, CancellationToken cancellationToken)
    {
        var comments = new List<JiraComment>();
        var startAt = 0;

        while (true)
        {
            var relativeUrl = $"rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/comment?startAt={startAt}&maxResults=100";
            var page = await GetDocumentAsync(relativeUrl, issueKey, "comentarios", cancellationToken);

            if (!page.TryGetProperty("comments", out var pageComments) || pageComments.ValueKind is not JsonValueKind.Array)
            {
                return comments;
            }

            foreach (var comment in pageComments.EnumerateArray())
            {
                comments.Add(new JiraComment(
                    GetString(comment, "id") ?? string.Empty,
                    MapUser(GetPropertyOrDefault(comment, "author")),
                    ToPlainText(GetPropertyOrDefault(comment, "body")),
                    GetDateTimeOffset(comment, "created"),
                    GetDateTimeOffset(comment, "updated")));
            }

            var total = GetInt32(page, "total");
            if (comments.Count >= total || pageComments.GetArrayLength() is 0)
            {
                return comments;
            }

            startAt += pageComments.GetArrayLength();
        }
    }

    private async Task<JsonElement> GetDocumentAsync(
        string relativeUrl,
        string issueKey,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var requestBaseUrl = await GetRequestBaseUrlAsync(cancellationToken);
        var response = await SendGetAsync(requestBaseUrl, relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode
            && _usesAtlassianApiProxy
            && response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            var directResponse = await SendGetAsync(_siteBaseUrl, relativeUrl, cancellationToken);
            if (directResponse.IsSuccessStatusCode)
            {
                _requestBaseUrl = _siteBaseUrl;
                _usesAtlassianApiProxy = false;
                return ParseDocument(directResponse.Content);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new JiraApiException(
                response.StatusCode,
                BuildErrorMessage(issueKey, resourceName, response.StatusCode, response.Content));
        }

        return ParseDocument(response.Content);
    }

    private async Task<JiraHttpResponse> SendGetAsync(
        Uri baseUrl,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl, relativeUrl));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _credentials.ToBasicAuthenticationParameter());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new JiraHttpResponse(response.StatusCode, content);
    }

    private async Task<Uri> GetRequestBaseUrlAsync(CancellationToken cancellationToken)
    {
        if (_requestBaseUrl is not null)
        {
            return _requestBaseUrl;
        }

        var cloudId = await TryGetCloudIdAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(cloudId))
        {
            _requestBaseUrl = new Uri($"https://api.atlassian.com/ex/jira/{Uri.EscapeDataString(cloudId)}/", UriKind.Absolute);
            _usesAtlassianApiProxy = true;
            return _requestBaseUrl;
        }

        _requestBaseUrl = _siteBaseUrl;
        _usesAtlassianApiProxy = false;
        return _requestBaseUrl;
    }

    private async Task<string?> TryGetCloudIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_siteBaseUrl, "_edge/tenant_info"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            return GetString(document.RootElement, "cloudId");
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement ParseDocument(string content)
    {
        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }

    private IReadOnlyList<JiraAttachment> GetAttachments(JsonElement attachments)
    {
        if (attachments.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return attachments.EnumerateArray()
            .Select(attachment =>
            {
                var id = GetString(attachment, "id") ?? string.Empty;
                var contentUrl = GetString(attachment, "content")
                    ?? new Uri(_siteBaseUrl, $"rest/api/3/attachment/content/{Uri.EscapeDataString(id)}").ToString();

                return new JiraAttachment(
                    id,
                    GetString(attachment, "filename") ?? string.Empty,
                    GetString(attachment, "mimeType"),
                    GetInt64(attachment, "size"),
                    GetDateTimeOffset(attachment, "created"),
                    MapUser(GetPropertyOrDefault(attachment, "author")),
                    contentUrl);
            })
            .ToArray();
    }

    private static Uri NormalizeBaseUrl(Uri baseUrl)
    {
        if (!baseUrl.IsAbsoluteUri || baseUrl.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("JIRA_BASE_URL deve ser uma URL HTTP ou HTTPS valida.", nameof(baseUrl));
        }

        var value = baseUrl.ToString().TrimEnd('/');
        const string apiSuffix = "/rest/api/3";

        if (value.EndsWith(apiSuffix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^apiSuffix.Length];
        }

        return new Uri($"{value}/", UriKind.Absolute);
    }

    private static string BuildErrorMessage(string issueKey, string resourceName, System.Net.HttpStatusCode statusCode, string content)
    {
        var details = TryGetErrorDetails(content);
        var suffix = string.IsNullOrWhiteSpace(details) ? string.Empty : $" Detalhes: {details}";
        return $"Jira retornou {(int)statusCode} ao consultar {resourceName} do card {issueKey}.{suffix}";
    }

    private static string? TryGetErrorDetails(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var messages = new List<string>();

            if (root.TryGetProperty("errorMessages", out var errorMessages) && errorMessages.ValueKind is JsonValueKind.Array)
            {
                messages.AddRange(errorMessages.EnumerateArray()
                    .Where(item => item.ValueKind is JsonValueKind.String)
                    .Select(item => item.GetString()!)
                    .Where(message => !string.IsNullOrWhiteSpace(message)));
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind is JsonValueKind.Object)
            {
                messages.AddRange(errors.EnumerateObject()
                    .Select(property => property.Value.GetString())
                    .Where(message => !string.IsNullOrWhiteSpace(message))!);
            }

            return messages.Count is 0 ? null : string.Join(" ", messages);
        }
        catch (JsonException)
        {
            return content.Trim();
        }
    }

    private static JiraUser? MapUser(JsonElement user)
    {
        if (user.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        var displayName = GetString(user, "displayName");
        return string.IsNullOrWhiteSpace(displayName)
            ? null
            : new JiraUser(displayName, GetString(user, "accountId"));
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement values)
    {
        return values.ValueKind is not JsonValueKind.Array
            ? []
            : values.EnumerateArray()
                .Where(value => value.ValueKind is JsonValueKind.String)
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
    }

    private static IReadOnlyList<string> GetNamedArray(JsonElement values)
    {
        return values.ValueKind is not JsonValueKind.Array
            ? []
            : values.EnumerateArray()
                .Select(value => GetString(value, "name"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
    }

    private static JsonElement GetPropertyOrDefault(JsonElement element, string propertyName)
    {
        return element.ValueKind is JsonValueKind.Object && element.TryGetProperty(propertyName, out var value)
            ? value
            : default;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return GetPropertyOrDefault(element, propertyName).ValueKind is JsonValueKind.String
            ? GetPropertyOrDefault(element, propertyName).GetString()
            : null;
    }

    private static int GetInt32(JsonElement element, string propertyName)
    {
        var property = GetPropertyOrDefault(element, propertyName);
        return property.ValueKind is JsonValueKind.Number && property.TryGetInt32(out var value) ? value : 0;
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        var property = GetPropertyOrDefault(element, propertyName);
        return property.ValueKind is JsonValueKind.Number && property.TryGetInt64(out var value) ? value : null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement element, string propertyName)
    {
        var property = GetPropertyOrDefault(element, propertyName);
        return property.ValueKind is JsonValueKind.String && property.TryGetDateTimeOffset(out var value) ? value : null;
    }

    private static string ToPlainText(JsonElement document)
    {
        if (document.ValueKind is JsonValueKind.String)
        {
            return document.GetString() ?? string.Empty;
        }

        if (document.ValueKind is not JsonValueKind.Object)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendText(document, builder);
        return builder.ToString().Trim();
    }

    private static void AppendText(JsonElement node, StringBuilder builder)
    {
        var type = GetString(node, "type");

        if (type is "text")
        {
            builder.Append(GetString(node, "text"));
            return;
        }

        if (type is "hardBreak")
        {
            builder.AppendLine();
            return;
        }

        if (type is "mention" or "emoji")
        {
            builder.Append(GetString(GetPropertyOrDefault(node, "attrs"), "text"));
            return;
        }

        if (type is "inlineCard")
        {
            builder.Append(GetString(GetPropertyOrDefault(node, "attrs"), "url"));
            return;
        }

        var beforeLength = builder.Length;
        var content = GetPropertyOrDefault(node, "content");
        if (content.ValueKind is JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                AppendText(child, builder);
            }
        }

        if (builder.Length > beforeLength && type is "paragraph" or "heading" or "blockquote" or "listItem" or "tableRow")
        {
            builder.AppendLine();
        }
    }

    private sealed record JiraHttpResponse(System.Net.HttpStatusCode StatusCode, string Content)
    {
        public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
    }
}
