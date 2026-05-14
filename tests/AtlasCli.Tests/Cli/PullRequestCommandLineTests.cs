using AtlasCli.Cli;

namespace AtlasCli.Tests.Cli;

public sealed class PullRequestCommandLineTests
{
    [Fact]
    public void ShouldReturnGeneralHelpWhenNoArgumentsAreProvided()
    {
        var result = PullRequestCommandLine.Parse([]);

        Assert.True(result.IsHelp);
        Assert.Null(result.HelpCommand);
    }

    [Fact]
    public void ShouldReturnGeneralHelpWhenHelpOptionIsProvided()
    {
        var result = PullRequestCommandLine.Parse(["--help"]);

        Assert.True(result.IsHelp);
        Assert.Null(result.HelpCommand);
    }

    [Fact]
    public void ShouldReturnCommandHelpWhenCommandHelpIsProvided()
    {
        var result = PullRequestCommandLine.Parse(["bb-get-pr-reports", "--help"]);

        Assert.True(result.IsHelp);
        Assert.Equal(PullRequestCommandKind.GetReports, result.HelpCommand);
    }

    [Fact]
    public void ShouldParseCommentsCommandWhenCommandIsGetPrComments()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(PullRequestCommandKind.GetComments, result.Options!.Command);
    }

    [Fact]
    public void ShouldParseTasksCommandWhenCommandIsGetPrTasks()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-tasks", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(PullRequestCommandKind.GetTasks, result.Options!.Command);
    }

    [Fact]
    public void ShouldParseReportsCommandWhenCommandIsGetPrReports()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-reports", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(PullRequestCommandKind.GetReports, result.Options!.Command);
    }

    [Fact]
    public void ShouldParseBranchesCommandWhenCommandIsGetPrBranches()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-branches", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(PullRequestCommandKind.GetBranches, result.Options!.Command);
    }

    [Fact]
    public void ShouldParsePipelineLogCommandWhenCommandIsGetPrPipelineLog()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-pipeline-log", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(PullRequestCommandKind.GetPipelineLog, result.Options!.Command);
    }

    [Fact]
    public void ShouldParseLatestCommitPipelineWhenPipelineLogOptionIsProvided()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-pipeline-log", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--latest-commit-pipeline"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.True(result.Options!.LatestCommitPipeline);
        Assert.Null(result.Options.BuildNumber);
    }

    [Fact]
    public void ShouldParseBuildNumberWhenPipelineBuildIsProvided()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-pipeline-log", "--repo", "workspace/repo", "--build", "42"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Options!.BuildNumber);
        Assert.False(result.Options.LatestCommitPipeline);
    }

    [Fact]
    public void ShouldRejectPipelineStrategiesWhenBuildAndLatestCommitAreProvidedTogether()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-pipeline-log", "--repo", "workspace/repo", "--build", "42", "--latest-commit-pipeline"]);

        Assert.False(result.IsHelp);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);
        Assert.Equal("Informe apenas uma estrategia de pipeline: --latest-commit-pipeline ou --build.", result.Error.Message);
    }

    [Fact]
    public void ShouldRejectPipelineOnlyOptionsWhenCommandIsNotPipelineLog()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--build", "42"]);

        Assert.False(result.IsHelp);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);
        Assert.Equal("--latest-commit-pipeline e --build sao suportados apenas por bb-get-pr-pipeline-log.", result.Error.Message);
    }

    [Fact]
    public void ShouldParseJsonOutputWhenOutputIsJson()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--output", "json"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.Equal(OutputFormat.Json, result.Options!.OutputFormat);
    }

    [Fact]
    public void ShouldParseIncludeSystemWhenFlagIsProvided()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--include-system"]);

        Assert.False(result.IsHelp);
        Assert.Null(result.Error);
        Assert.True(result.Options!.IncludeSystem);
    }

    [Fact]
    public void ShouldRejectUnknownParameterWhenArgumentIsNotSupported()
    {
        var result = PullRequestCommandLine.Parse(
            ["bb-get-pr-comments", "--pr", "https://bitbucket.org/workspace/repo/pull-requests/1", "--unknown"]);

        Assert.False(result.IsHelp);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);
    }
}
