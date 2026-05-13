using System.Text.Json;

namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReport(
    string Id,
    string Title,
    string Reporter,
    string ReportType,
    string Result,
    string? Link,
    string? Details,
    IReadOnlyList<PullRequestReportData> Data);

public sealed record PullRequestReportData(
    string Title,
    string Type,
    JsonElement? Value);
