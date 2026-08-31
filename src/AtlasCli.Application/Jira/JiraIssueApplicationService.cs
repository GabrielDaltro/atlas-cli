namespace AtlasCli.Application.Jira;

public sealed class JiraIssueApplicationService
{
    private readonly IJiraIssueGateway _gateway;

    public JiraIssueApplicationService(IJiraIssueGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        return _gateway.GetIssueAsync(issueKey, cancellationToken);
    }
}
