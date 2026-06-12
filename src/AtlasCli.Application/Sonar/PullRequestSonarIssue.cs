namespace AtlasCli.Application.Sonar;

public sealed record PullRequestSonarIssue(
    string AnnotationId,
    string Key,
    string Type,
    string Severity,
    string File,
    int? Line,
    string Message,
    string Link,
    string ReportId,
    string ReportTitle,
    string? CreatedOn,
    string? UpdatedOn);
