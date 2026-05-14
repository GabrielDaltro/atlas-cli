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

        if (!TryResolveBitbucketContext(parseResult.Options, out var repository, out var pullRequest, out var validationError))
        {
            await ErrorOutputWriter.WriteAsync(
                validationError!,
                parseResult.Options.OutputFormat,
                standardError);
            return 1;
        }

        if (!BitbucketCredentials.TryFromEnvironment(environment.GetVariable, repository!.Workspace, parseResult.Options.TokenEnvironmentSuffix, out var credentials, out var credentialError))
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
        var pipelineBuildResolver = new BitbucketPullRequestPipelineBuildResolver(
            bitbucketClient,
            environment.GetVariable,
            baseUrl);
        var pullRequestApplicationService = new PullRequestApplicationService(
            bitbucketClient,
            sourceCommitResolver,
            pipelineBuildResolver);

        try
        {
            if (parseResult.Options.Command is PullRequestCommandKind.GetReports)
            {
                var reports = await pullRequestApplicationService.GetReportsAsync(pullRequest!, cancellationToken);
                await ReportOutputWriter.WriteAsync(reports, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            if (parseResult.Options.Command is PullRequestCommandKind.GetBranches)
            {
                var branches = await pullRequestApplicationService.GetBranchesAsync(pullRequest!, cancellationToken);
                await BranchOutputWriter.WriteAsync(branches, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            if (parseResult.Options.Command is PullRequestCommandKind.GetPipelineLog)
            {
                PullRequestPipelineLog pipelineLog;

                if (parseResult.Options.BuildNumber is int buildNumber)
                {
                    pipelineLog = await pullRequestApplicationService.GetPipelineLogByBuildNumberAsync(repository, buildNumber, cancellationToken);
                }
                else if (parseResult.Options.LatestCommitPipeline)
                {
                    pipelineLog = await pullRequestApplicationService.GetLatestPipelineLogAsync(pullRequest!, cancellationToken);
                }
                else
                {
                    pipelineLog = await pullRequestApplicationService.GetReferencedPipelineLogAsync(pullRequest!, cancellationToken);
                }

                await PipelineLogOutputWriter.WriteAsync(pipelineLog, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            if (parseResult.Options.Command is PullRequestCommandKind.GetTasks)
            {
                var tasks = await pullRequestApplicationService.GetTasksAsync(pullRequest!, cancellationToken);
                await TaskOutputWriter.WriteAsync(tasks, parseResult.Options.OutputFormat, standardOutput);
                return 0;
            }

            var comments = await pullRequestApplicationService.GetCommentsAsync(pullRequest!, cancellationToken);
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
            PullRequestCommandKind.GetBranches => GetBranchesHelpText,
            PullRequestCommandKind.GetPipelineLog => GetPipelineLogHelpText,
            _ => GeneralHelpText
        };
    }

    private const string GeneralHelpText = """
        atlascli

        Comandos:
          bb-get-pr-comments   Le comentarios de um pull request.
          bb-get-pr-tasks      Le tasks de um pull request.
          bb-get-pr-reports    Le reports de Code Insights do commit de origem do pull request.
          bb-get-pr-branches   Le as branches de origem e destino de um pull request.
          bb-get-pr-pipeline-log Le o log do pipeline referenciado no PR, do ultimo pipeline do commit ou de um build especifico.

        Formas equivalentes:
          atlascli bb-get-pr-comments ...
          atlascli bb get-pr-comments ...

        Ajuda por comando:
          atlascli bb-get-pr-comments --help
          atlascli bb-get-pr-tasks --help
          atlascli bb-get-pr-reports --help
          atlascli bb-get-pr-branches --help
          atlascli bb-get-pr-pipeline-log --help

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

    private const string GetBranchesHelpText = """
        bb-get-pr-branches

        Le as branches de origem e destino de um pull request.

        Uso:
          atlascli bb-get-pr-branches --repo <workspace/repositorio> --pr <numero-do-pr> [--output table|json]
          atlascli bb-get-pr-branches --pr <url-do-pr> [--output table|json]
          atlascli bb-get-pr-branches --url <url-do-pr> [--output table|json]

        Opcoes:
          --output <table|json>           Formato de saida. Padrao: table.

        Variaveis:
          BITBUCKET_<WORKSPACE>_EMAIL
          BB_<WORKSPACE>_GET_PR_BRANCHES_TOKEN

        Exemplo:
          atlascli bb-get-pr-branches --pr "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682" --output json
        """;

    private const string GetPipelineLogHelpText = """
        bb-get-pr-pipeline-log

        Le o log do pipeline referenciado no PR, do ultimo pipeline do commit de origem ou de um build especifico.

        Uso:
          atlascli bb-get-pr-pipeline-log --repo <workspace/repositorio> --pr <numero-do-pr> [--output table|json]
          atlascli bb-get-pr-pipeline-log --pr <url-do-pr> [--output table|json]
          atlascli bb-get-pr-pipeline-log --url <url-do-pr> [--output table|json]
          atlascli bb-get-pr-pipeline-log --pr <url-do-pr> --latest-commit-pipeline [--output table|json]
          atlascli bb-get-pr-pipeline-log --repo <workspace/repositorio> --build <numero-do-build> [--output table|json]

        Opcoes:
          --latest-commit-pipeline       Busca o ultimo pipeline do commit de origem. Padrao: usa o pipeline referenciado no PR.
          --build <numero-do-build>      Busca um build especifico. Requer --repo ou um PR informado por --pr/--url.
          --output <table|json>           Formato de saida. Padrao: table.

        Variaveis:
          BITBUCKET_<WORKSPACE>_EMAIL
          BB_<WORKSPACE>_GET_PR_PIPELINE_LOG_TOKEN

        Observacao:
          Este comando precisa de acesso de leitura a pipelines.
          Quando o token principal nao tiver read:pullrequest:bitbucket, o CLI pode reutilizar BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN para ler metadados do PR e descobrir o pipeline referenciado.

        Exemplo:
          atlascli bb-get-pr-pipeline-log --pr "https://bitbucket.org/dynamoxteam/dotnet-apps-common-libs/pull-requests/682" --output json
        """;

    private static bool TryResolveBitbucketContext(
        PullRequestCommandOptions options,
        out RepositoryReference? repository,
        out PullRequestReference? pullRequest,
        out CliError? error)
    {
        repository = null;
        pullRequest = null;
        error = null;

        if (options.Command is PullRequestCommandKind.GetPipelineLog && options.BuildNumber is not null)
        {
            if (!string.IsNullOrWhiteSpace(options.PullRequest) || !string.IsNullOrWhiteSpace(options.Url))
            {
                var pullRequestResult = PullRequestReferenceParser.Parse(new PullRequestReferenceInput(
                    options.Repository,
                    options.PullRequest,
                    options.Url));

                if (!pullRequestResult.IsSuccess)
                {
                    error = new CliError(ErrorCodes.ValidationError, pullRequestResult.ErrorMessage!);
                    return false;
                }

                pullRequest = pullRequestResult.Reference!;
                repository = pullRequest.ToRepositoryReference();
                return true;
            }

            var repositoryResult = PullRequestReferenceParser.ParseRepository(options.Repository);
            if (!repositoryResult.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(options.Repository)
                    ? "--build exige --repo <workspace/repositorio> ou um PR informado por --pr/--url."
                    : repositoryResult.ErrorMessage!;
                error = new CliError(ErrorCodes.ValidationError, message);
                return false;
            }

            repository = repositoryResult.Reference!;
            return true;
        }

        var referenceResult = PullRequestReferenceParser.Parse(new PullRequestReferenceInput(
            options.Repository,
            options.PullRequest,
            options.Url));

        if (!referenceResult.IsSuccess)
        {
            error = new CliError(ErrorCodes.ValidationError, referenceResult.ErrorMessage!);
            return false;
        }

        pullRequest = referenceResult.Reference!;
        repository = pullRequest.ToRepositoryReference();
        return true;
    }
}
