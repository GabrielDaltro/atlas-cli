namespace AtlasCli.Cli;

public sealed record CliError(string Code, string Message);

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AuthenticationOrConfigurationError = "AUTHENTICATION_OR_CONFIGURATION_ERROR";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string BitbucketApiError = "BITBUCKET_API_ERROR";
    public const string UnexpectedError = "UNEXPECTED_ERROR";
}
