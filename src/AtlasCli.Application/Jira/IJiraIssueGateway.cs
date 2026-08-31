namespace AtlasCli.Application.Jira;

public interface IJiraIssueGateway
{
    Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default);
}
