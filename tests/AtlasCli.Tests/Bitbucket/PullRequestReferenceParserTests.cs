using AtlasCli.Application.Bitbucket;

namespace AtlasCli.Tests.Bitbucket;

public sealed class PullRequestReferenceParserTests
{
    [Fact]
    public void ShouldParseWorkspaceRepositoryAndNumberWhenPrIsBitbucketUrl()
    {
        var input = new PullRequestReferenceInput(
            Repository: null,
            PullRequest: "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682",
            Url: null);

        var result = PullRequestReferenceParser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        var reference = result.Reference!;
        Assert.Equal("dynamoxteam", reference.Workspace);
        Assert.Equal("dotnet-apps-common-libs", reference.Repository);
        Assert.Equal(682, reference.Number);
    }

    [Fact]
    public void ShouldParseWorkspaceRepositoryAndNumberWhenUrlOptionIsBitbucketUrl()
    {
        var input = new PullRequestReferenceInput(
            Repository: null,
            PullRequest: null,
            Url: "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682");

        var result = PullRequestReferenceParser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        var reference = result.Reference!;
        Assert.Equal("dynamoxteam", reference!.Workspace);
        Assert.Equal("dotnet-apps-common-libs", reference.Repository);
        Assert.Equal(682, reference.Number);
    }

    [Fact]
    public void ShouldParseWorkspaceRepositoryAndNumberWhenRepoAndPrNumberAreProvided()
    {
        var input = new PullRequestReferenceInput(
            Repository: "dynamoxteam/dotnet-apps-common-libs",
            PullRequest: "682",
            Url: null);

        var result = PullRequestReferenceParser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        var reference = result.Reference!;
        Assert.Equal("dynamoxteam", reference!.Workspace);
        Assert.Equal("dotnet-apps-common-libs", reference.Repository);
        Assert.Equal(682, reference.Number);
    }

    [Fact]
    public void ShouldRejectPullRequestNumberWhenRepositoryIsMissing()
    {
        var input = new PullRequestReferenceInput(
            Repository: null,
            PullRequest: "682",
            Url: null);

        var result = PullRequestReferenceParser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Reference);
        Assert.Equal("--repo e obrigatorio quando --pr recebe apenas o numero do PR.", result.ErrorMessage);
    }

    [Fact]
    public void ShouldRejectPrAndUrlWhenBothAreProvided()
    {
        var input = new PullRequestReferenceInput(
            Repository: null,
            PullRequest: "682",
            Url: "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682");

        var result = PullRequestReferenceParser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Reference);
        Assert.Equal("Informe apenas uma identificacao de PR: --url ou --pr.", result.ErrorMessage);
    }
}
