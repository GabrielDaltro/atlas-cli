using AtlasCli.Cli;

namespace AtlasCli.Tests.Cli;

public sealed class DictionaryEnvironment : IEnvironment
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public DictionaryEnvironment(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public string? GetVariable(string name)
    {
        return _values.GetValueOrDefault(name);
    }
}
