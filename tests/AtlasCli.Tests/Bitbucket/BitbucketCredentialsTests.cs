using AtlasCli.Infrastructure.Bitbucket;
using AtlasCli.Tests.Cli;

namespace AtlasCli.Tests.Bitbucket;

public sealed class BitbucketCredentialsTests
{
    [Fact]
    public void ShouldLoadWorkspaceScopedCredentialsWhenConfigured()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_DYNAMOXTEAM_EMAIL"] = "developer@example.com",
            ["BB_DYNAMOXTEAM_GET_PR_COMMENTS_TOKEN"] = "token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_COMMENTS_TOKEN",
            out var credentials,
            out var error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("developer@example.com", credentials!.Login);
        Assert.Equal("token", credentials.Token);
    }

    [Fact]
    public void ShouldLoadTaskTokenWhenTaskTokenSuffixIsRequested()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_DYNAMOXTEAM_EMAIL"] = "developer@example.com",
            ["BB_DYNAMOXTEAM_GET_PR_TASKS_TOKEN"] = "task-token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_TASKS_TOKEN",
            out var credentials,
            out var error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("task-token", credentials!.Token);
    }

    [Fact]
    public void ShouldLoadReportsTokenWhenReportsTokenSuffixIsRequested()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_DYNAMOXTEAM_EMAIL"] = "developer@example.com",
            ["BB_DYNAMOXTEAM_GET_PR_REPORTS_TOKEN"] = "reports-token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_REPORTS_TOKEN",
            out var credentials,
            out var error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("reports-token", credentials!.Token);
    }

    [Fact]
    public void ShouldLoadBranchesTokenWhenBranchesTokenSuffixIsRequested()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_DYNAMOXTEAM_EMAIL"] = "developer@example.com",
            ["BB_DYNAMOXTEAM_GET_PR_BRANCHES_TOKEN"] = "branches-token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_BRANCHES_TOKEN",
            out var credentials,
            out var error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("branches-token", credentials!.Token);
    }

    [Fact]
    public void ShouldLoadPipelineLogTokenWhenPipelineLogTokenSuffixIsRequested()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_DYNAMOXTEAM_EMAIL"] = "developer@example.com",
            ["BB_DYNAMOXTEAM_GET_PR_PIPELINE_LOG_TOKEN"] = "pipeline-log-token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_PIPELINE_LOG_TOKEN",
            out var credentials,
            out var error);

        Assert.True(loaded);
        Assert.Null(error);
        Assert.Equal("pipeline-log-token", credentials!.Token);
    }

    [Fact]
    public void ShouldConvertHyphenatedWorkspaceToEnvironmentKeyWhenWorkspaceHasHyphen()
    {
        var key = BitbucketCredentials.ToEnvironmentKey("my-team");

        Assert.Equal("MY_TEAM", key);
    }

    [Fact]
    public void ShouldRejectGlobalCredentialsWhenWorkspaceScopedCredentialsAreMissing()
    {
        var environment = new DictionaryEnvironment(new Dictionary<string, string>
        {
            ["BITBUCKET_EMAIL"] = "developer@example.com",
            ["BB_GET_PR_COMMENTS_TOKEN"] = "global-token"
        });

        var loaded = BitbucketCredentials.TryFromEnvironment(
            environment.GetVariable,
            "dynamoxteam",
            "GET_PR_COMMENTS_TOKEN",
            out var credentials,
            out var error);

        Assert.False(loaded);
        Assert.Null(credentials);
        Assert.Equal("Configure BITBUCKET_DYNAMOXTEAM_EMAIL para autenticar no workspace dynamoxteam.", error);
    }
}
