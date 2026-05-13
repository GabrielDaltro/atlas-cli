using System.Text;

namespace AtlasCli.Infrastructure.Bitbucket;

public sealed record BitbucketCredentials(string Login, string Token)
{
    public string ToBasicAuthenticationParameter()
    {
        var rawCredentials = $"{Login}:{Token}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));
    }

    public static bool TryFromEnvironment(
        Func<string, string?> getEnvironmentVariable,
        string workspace,
        string tokenEnvironmentSuffix,
        out BitbucketCredentials? credentials,
        out string? errorMessage)
    {
        var workspaceKey = ToEnvironmentKey(workspace);
        var emailVariable = $"BITBUCKET_{workspaceKey}_EMAIL";
        var tokenVariable = $"BB_{workspaceKey}_{tokenEnvironmentSuffix}";
        var login = getEnvironmentVariable(emailVariable);
        var token = getEnvironmentVariable(tokenVariable);

        credentials = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(login))
        {
            errorMessage = $"Configure {emailVariable} para autenticar no workspace {workspace}.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            errorMessage = $"Configure {tokenVariable} com um token de leitura de pull requests para o workspace {workspace}.";
            return false;
        }

        credentials = new BitbucketCredentials(login, token);
        return true;
    }

    public static string ToEnvironmentKey(string workspace)
    {
        var builder = new StringBuilder(workspace.Length);

        foreach (var character in workspace)
        {
            builder.Append(char.IsLetterOrDigit(character)
                ? char.ToUpperInvariant(character)
                : '_');
        }

        return builder.ToString();
    }
}
