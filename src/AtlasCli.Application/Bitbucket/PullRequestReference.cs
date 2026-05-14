namespace AtlasCli.Application.Bitbucket;

public sealed record PullRequestReference(string Workspace, string Repository, int Number)
{
    public RepositoryReference ToRepositoryReference()
    {
        return new RepositoryReference(Workspace, Repository);
    }
}
