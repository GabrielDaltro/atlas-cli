using System.Text;

namespace AtlasCli.Infrastructure.Jira;

public sealed record JiraCredentials(string Email, string ApiToken)
{
    public string ToBasicAuthenticationParameter()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Email}:{ApiToken}"));
    }

    public static bool TryFromEnvironment(
        Func<string, string?> getEnvironmentVariable,
        out JiraCredentials? credentials,
        out string? errorMessage)
    {
        var email = getEnvironmentVariable("JIRA_EMAIL");
        var apiToken = getEnvironmentVariable("JIRA_API_TOKEN");

        credentials = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            errorMessage = "Configure JIRA_EMAIL para autenticar no Jira.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            errorMessage = "Configure JIRA_API_TOKEN com um API token que tenha acesso de leitura ao card.";
            return false;
        }

        credentials = new JiraCredentials(email, apiToken);
        return true;
    }
}
