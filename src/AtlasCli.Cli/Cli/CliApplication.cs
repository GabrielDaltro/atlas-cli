using AtlasCli.Application.Bitbucket;
using AtlasCli.Infrastructure.Bitbucket;
using AtlasCli.Cli.Output;

namespace AtlasCli.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(
        string[] args,
        IEnvironment environment,
        TextWriter standardOutput,
        TextWriter standardError,
        HttpMessageHandler? httpMessageHandler = null,
        CancellationToken cancellationToken = default)
    {
        var parseResult = PullRequestCommandLine.Parse(args);

        if (parseResult.IsHelp)
        {
            await standardOutput.WriteLineAsync(GetHelpText(parseResult.HelpCommand));
            return 0;
        }

        if (parseResult.Error is not null || parseResult.Options is null)
        {
            await ErrorOutputWriter.WriteAsync(
                parseResult.Error ?? new CliError(ErrorCodes.ValidationError, "Parametros invalidos."),
                parseResult.OutputFormat,
                standardError);
            return 1;
        }

        var referenceResult = PullRequestReferenceParser.Parse(new PullRequestReferenceInput(
            parseResult.Options.Repository,
            parseResult.Options.PullRequest,
            parseResult.Options.Url));

        if (!referenceResult.IsSuccess)
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.ValidationError, referenceResult.ErrorMessage!),
                parseResult.Options.OutputFormat,
                standardError);
            return 1;
        }

        var pullRequest = referenceResult.Reference!;

        if (!BitbucketCredentials.TryFromEnvironment(environment.GetVariable, pullRequest.Workspace, parseResult.Options.TokenEnvironmentSuffix, out var credentials, out var credentialError))
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.AuthenticationOrConfigurationError, credentialError!),
                parseResult.Options.OutputFormat,
                standardError);
            return 2;
        }

        var baseUrl = environment.GetVariable("BITBUCKET_BASE_URL");

        using var httpClient = httpMessageHandler is null
            ? new HttpClient()
            : new HttpClient(httpMessageHandler);

        var bitbucketClient = new BitbucketClient(httpClient, baseUrl, credentials!);
        var sourceCommitResolver = new BitbucketPullRequestSourceCommitResolver(
            bitbucketClient,
            environment.GetVariable,
            baseUrl);
        var pullRequestApplicationService = new PullRequestApplicationService(
            bitbucketClient,
            sourceCommitResolver);

        try
        {
            if (parseResult.Options.Command is PullRequestCommandKind.GetReports)
            {
                var reports = await pullRequestApplicationService.GetReportsAsync(pullRequest, cancellationToken);
                await ReportOutputWriter.WriteAsync(reports, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            if (parseResult.Options.Command is PullRequestCommandKind.GetTasks)
            {
                var tasks = await pullRequestApplicationService.GetTasksAsync(pullRequest, cancellationToken);
                await TaskOutputWriter.WriteAsync(tasks, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            var comments = await pullRequestApplicationService.GetCommentsAsync(pullRequest, cancellationToken);
            await CommentOutputWriter.WriteAsync(comments, parseResult.Options.OutputFormat, standardOutput);
            return 0;
        }
        catch (BitbucketApiException exception) when (exception.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.AuthenticationOrConfigurationError, exception.Message),
                parseResult.Options.OutputFormat,
                standardError);
            return 2;
        }
        catch (BitbucketApiException exception) when (exception.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.ResourceNotFound, exception.Message),
                parseResult.Options.OutputFormat,
                standardError);
            return 3;
        }
        catch (BitbucketApiException exception)
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.BitbucketApiError, exception.Message),
                parseResult.Options.OutputFormat,
                standardError);
            return 5;
        }
        catch (Exception exception)
        {
            await ErrorOutputWriter.WriteAsync(
                new CliError(ErrorCodes.UnexpectedError, exception.Message),
                parseResult.Options.OutputFormat,
                standardError);
            return 5;
        }
    }

    private static string GetHelpText(PullRequestCommandKind? command)
    {
        return command switch
        {
            PullRequestCommandKind.GetComments => GetCommentsHelpText,
            PullRequestCommandKind.GetTasks => GetTasksHelpText,
            PullRequestCommandKind.GetReports => GetReportsHelpText,
            _ => GeneralHelpText
        };
    }

    private const string GeneralHelpText = """
        atlascli

        Comandos:
          bb-get-pr-comments   Le comentarios de um pull request.
          bb-get-pr-tasks      Le tasks de um pull request.
          bb-get-pr-reports    Le reports de Code Insights do commit de origem do pull request.

        Formas equivalentes:
          atlascli bb-get-pr-comments ...
          atlascli bb get-pr-comments ...

        Ajuda por comando:
          atlascli bb-get-pr-comments --help
          atlascli bb-get-pr-tasks --help
          atlascli bb-get-pr-reports --help

        Opcoes comuns:
          --repo <workspace/repositorio>  Repositorio quando --pr recebe apenas o numero.
          --pr <numero-ou-url-do-pr>      Numero do PR ou URL completa do PR.
          --url <url-do-pr>               Alias explicito para URL do PR.
          --output <table|json>           Formato de saida. Padrao: table.
          --help, -h                      Mostra ajuda.
        """;

    private const string GetCommentsHelpText = """
        bb-get-pr-comments

        Le comentarios de um pull request.

        Uso:
          atlascli bb-get-pr-comments --repo <workspace/repositorio> --pr <numero-do-pr> [--output table|json]
          atlascli bb-get-pr-comments --pr <url-do-pr> [--output table|json]
          atlascli bb-get-pr-comments --url <url-do-pr> [--output table|json]

        Opcoes:
          --include-system                Inclui comentarios de sistema, quando a API retornar esse tipo de item.
          --output <table|json>           Formato de saida. Padrao: table.

        Variaveis:
          BITBUCKET_<WORKSPACE>_EMAIL
          BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN

        Exemplo:
          atlascli bb-get-pr-comments --pr "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682" --output json
        """;

    private const string GetTasksHelpText = """
        bb-get-pr-tasks

        Le tasks de um pull request.

        Uso:
          atlascli bb-get-pr-tasks --repo <workspace/repositorio> --pr <numero-do-pr> [--output table|json]
          atlascli bb-get-pr-tasks --pr <url-do-pr> [--output table|json]
          atlascli bb-get-pr-tasks --url <url-do-pr> [--output table|json]

        Opcoes:
          --output <table|json>           Formato de saida. Padrao: table.

        Variaveis:
          BITBUCKET_<WORKSPACE>_EMAIL
          BB_<WORKSPACE>_GET_PR_TASKS_TOKEN

        Exemplo:
          atlascli bb-get-pr-tasks --pr "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682" --output json
        """;

    private const string GetReportsHelpText = """
        bb-get-pr-reports

        Le reports de Code Insights associados ao commit de origem de um pull request.

        Uso:
          atlascli bb-get-pr-reports --repo <workspace/repositorio> --pr <numero-do-pr> [--output table|json]
          atlascli bb-get-pr-reports --pr <url-do-pr> [--output table|json]
          atlascli bb-get-pr-reports --url <url-do-pr> [--output table|json]

        Opcoes:
          --output <table|json>           Formato de saida. Padrao: table.

        Variaveis:
          BITBUCKET_<WORKSPACE>_EMAIL
          BB_<WORKSPACE>_GET_PR_REPORTS_TOKEN

        Observacao:
          Para descobrir o commit de origem, este comando precisa ler metadados do PR.
          Se o token de reports nao tiver read:pullrequest:bitbucket, o CLI tenta usar BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN para essa etapa.

        Exemplo:
          atlascli bb-get-pr-reports --pr "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682" --output json
        """;

}
