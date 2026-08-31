namespace AtlasCli.Application.Jira;

public sealed record JiraAttachment(
    string Id,
    string FileName,
    string? MediaType,
    long? Size,
    DateTimeOffset? CreatedAt,
    JiraUser? Author,
    string ContentUrl);
