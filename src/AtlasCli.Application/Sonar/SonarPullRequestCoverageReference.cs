namespace AtlasCli.Application.Sonar;

public sealed record SonarPullRequestCoverageReference(
    string BaseUrl,
    string ProjectKey,
    string PullRequest);
