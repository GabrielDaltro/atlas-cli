namespace AtlasCli.Application.Jira;

public sealed record JiraIssue(
    string Key,
    string Summary,
    string Description,
    string IssueType,
    string Status,
    string? Priority,
    JiraUser? Assignee,
    JiraUser? Reporter,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> Components,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<JiraComment> Comments,
    IReadOnlyList<JiraAttachment> Attachments,
    string Url);
