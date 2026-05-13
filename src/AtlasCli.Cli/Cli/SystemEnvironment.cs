namespace AtlasCli.Cli;

public sealed class SystemEnvironment : IEnvironment
{
    public string? GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
