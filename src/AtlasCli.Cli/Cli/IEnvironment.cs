namespace AtlasCli.Cli;

public interface IEnvironment
{
    string? GetVariable(string name);
}
