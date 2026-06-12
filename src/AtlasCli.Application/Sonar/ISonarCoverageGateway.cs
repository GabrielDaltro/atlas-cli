namespace AtlasCli.Application.Sonar;

public interface ISonarCoverageGateway
{
    Task<IReadOnlyList<PullRequestUncoveredLine>> GetNewUncoveredLinesAsync(
        SonarPullRequestCoverageReference reference,
        CancellationToken cancellationToken = default);
}
