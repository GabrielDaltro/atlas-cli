namespace AtlasCli.Application.Sonar;

public sealed class SonarConfigurationException : Exception
{
    public SonarConfigurationException(string message)
        : base(message)
    {
    }
}
