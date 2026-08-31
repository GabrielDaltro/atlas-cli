using AtlasCli.Cli;

namespace AtlasCli.Tests.Cli;

public sealed class JiraCommandLineTests
{
    [Fact]
    public void ShouldParseJiraIssueCommandWhenIssueKeyIsProvided()
    {
        var result = JiraCommandLine.Parse(["jira-get-issue", "--issue", "DYN-123", "--output", "json"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal("DYN-123", result.Options!.Issue);
        Assert.Equal(OutputFormat.Json, result.Options.OutputFormat);
    }

    [Fact]
    public void ShouldRejectJiraIssueCommandWhenIssueIsMissing()
    {
        var result = JiraCommandLine.Parse(["jira-get-issue"]);

        Assert.False(result.IsHelp);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);
        Assert.Equal("Informe --issue <chave-ou-url-do-card>.", result.Error.Message);
    }
}
