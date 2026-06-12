namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReportAnnotation(
    string Id,
    string ExternalId,
    string ReportId,
    string ReportTitle,
    string Type,
    string Severity,
    string File,
    int? Line,
    string Summary,
    string? Link,
    string? CreatedOn,
    string? UpdatedOn);
