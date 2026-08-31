namespace AtlasCli.Application.Jira;

public sealed record JiraComment(
    string Id,
    JiraUser? Author,
    string Body,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt);
