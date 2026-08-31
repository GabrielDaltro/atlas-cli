# Atlas CLI

CLI built with .NET to query pull request data from Bitbucket Cloud and card data from Jira Cloud.

The project currently supports:

- reading PR comments;
- reading PR tasks;
- reading Code Insights reports from the source commit;
- reading Sonar issues for a PR through the Sonar report linked in Code Insights;
- reading source and target branches from a PR;
- reading the unified diff from a PR;
- reading pipeline logs from the PR-linked pipeline, the latest source-commit pipeline, or a specific build.
- reading a Jira card, including its main fields, comments, and attachment metadata.

## Prerequisites

- .NET SDK 10
- access to Bitbucket Cloud
- access to Sonar when using commands that query Sonar directly
- access to Jira Cloud when using the Jira command
- the user email for the target workspace
- API tokens compatible with the endpoints you want to call

## Project structure

- [src/AtlasCli.Cli](/C:/Repositorios/jira-cli/src/AtlasCli.Cli)
- [src/AtlasCli.Application](/C:/Repositorios/jira-cli/src/AtlasCli.Application)
- [src/AtlasCli.Infrastructure.Bitbucket](/C:/Repositorios/jira-cli/src/AtlasCli.Infrastructure.Bitbucket)
- [src/AtlasCli.Infrastructure.Jira](/C:/Repositorios/jira-cli/src/AtlasCli.Infrastructure.Jira)
- [tests/AtlasCli.Tests](/C:/Repositorios/jira-cli/tests/AtlasCli.Tests)

## Local setup

From the repository root:

```powershell
dotnet restore AtlasCli.slnx
dotnet build AtlasCli.slnx
dotnet test AtlasCli.slnx
```

## Run without installing

The simplest development workflow is running the CLI directly with `dotnet run`:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- --help
```

To open help for a specific command:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-pipeline-log --help
```

## Publish the executable

If you want to use the CLI without typing `dotnet run` every time:

```powershell
dotnet publish src\AtlasCli.Cli\AtlasCli.Cli.csproj -c Release -o .\artifacts\atlascli
```

After that, on Windows:

```powershell
.\artifacts\atlascli\AtlasCli.exe --help
```

If you want to call `AtlasCli.exe` from any folder, add `.\artifacts\atlascli` to your user or system `PATH`.

## Environment variables

The CLI uses this naming pattern:

- `BITBUCKET_<WORKSPACE>_EMAIL`
- `BB_<WORKSPACE>_<TOKEN_SUFFIX>`

Example for the fictional workspace `acmeplatform`:

- `BITBUCKET_ACMEPLATFORM_EMAIL`
- `BB_ACMEPLATFORM_GET_PR_COMMENTS_TOKEN`
- `BB_ACMEPLATFORM_GET_PR_TASKS_TOKEN`
- `BB_ACMEPLATFORM_GET_PR_REPORTS_TOKEN`
- `BB_ACMEPLATFORM_GET_PR_BRANCHES_TOKEN`
- `BB_ACMEPLATFORM_GET_PR_DIFF_TOKEN`
- `BB_ACMEPLATFORM_GET_PR_PIPELINE_LOG_TOKEN`

Notes:

- the workspace name is normalized to uppercase;
- non-alphanumeric characters are converted to `_`;
- `BITBUCKET_BASE_URL` is optional and only matters if you want to override the default API base URL.
- `SONAR_TOKEN` is required by commands that query the Sonar API directly when the Sonar project is private.
- `JIRA_EMAIL` and `JIRA_API_TOKEN` authenticate Jira Cloud requests. API tokens with read-only scopes are supported and recommended.
- `JIRA_BASE_URL` is required when the Jira card is passed only by key, for example `https://company.atlassian.net`. It is optional when `--issue` receives the complete card URL.
- when using a scoped Jira token, the CLI obtains the site's Cloud ID from `/_edge/tenant_info` and calls Atlassian's API proxy automatically; no extra variable is required.

## Configure environment variables in PowerShell

### Temporary, current session only

```powershell
$env:BITBUCKET_ACMEPLATFORM_EMAIL = "you@company.com"
$env:BB_ACMEPLATFORM_GET_PR_COMMENTS_TOKEN = "your-token"
$env:BB_ACMEPLATFORM_GET_PR_TASKS_TOKEN = "your-token"
$env:BB_ACMEPLATFORM_GET_PR_REPORTS_TOKEN = "your-token"
$env:BB_ACMEPLATFORM_GET_PR_BRANCHES_TOKEN = "your-token"
$env:BB_ACMEPLATFORM_GET_PR_DIFF_TOKEN = "your-token"
$env:BB_ACMEPLATFORM_GET_PR_PIPELINE_LOG_TOKEN = "your-token"
$env:SONAR_TOKEN = "your-sonar-token"
$env:JIRA_BASE_URL = "https://company.atlassian.net"
$env:JIRA_EMAIL = "you@company.com"
$env:JIRA_API_TOKEN = "your-jira-api-token"
```

### Persistent for the current user

```powershell
setx BITBUCKET_ACMEPLATFORM_EMAIL "you@company.com"
setx BB_ACMEPLATFORM_GET_PR_COMMENTS_TOKEN "your-token"
setx BB_ACMEPLATFORM_GET_PR_TASKS_TOKEN "your-token"
setx BB_ACMEPLATFORM_GET_PR_REPORTS_TOKEN "your-token"
setx BB_ACMEPLATFORM_GET_PR_BRANCHES_TOKEN "your-token"
setx BB_ACMEPLATFORM_GET_PR_DIFF_TOKEN "your-token"
setx BB_ACMEPLATFORM_GET_PR_PIPELINE_LOG_TOKEN "your-token"
setx SONAR_TOKEN "your-sonar-token"
setx JIRA_BASE_URL "https://company.atlassian.net"
setx JIRA_EMAIL "you@company.com"
setx JIRA_API_TOKEN "your-jira-api-token"
```

After using `setx`, open a new terminal so the variables are available in the new session.

## Available commands

### Comments

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-comments --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

### Tasks

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-tasks --repo acmeplatform/service-platform --pr 684 --output json
```

### Reports

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-reports --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

### Sonar issues

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-sonar-issues --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

### Branches

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-branches --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

### Diff

Raw unified diff:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-diff --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684"
```

JSON envelope:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-diff --repo acmeplatform/service-platform --pr 684 --output json
```

### PR pipeline log

PR-linked pipeline:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-pipeline-log --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

Latest pipeline from the source commit:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-pipeline-log --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --latest-commit-pipeline --output json
```

Specific build:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- bb-get-pr-pipeline-log --repo acmeplatform/service-platform --build 2016 --output json
```

### Jira card

By issue key:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- jira-get-issue --issue DYN-123 --output json
```

Or by complete card URL, without `JIRA_BASE_URL`:

```powershell
dotnet run --project src\AtlasCli.Cli\AtlasCli.Cli.csproj -- jira-get-issue --issue "https://company.atlassian.net/browse/DYN-123" --output json
```

## Input conventions

- `--pr` accepts either a PR number or a full PR URL;
- if `--pr` receives only a number, `--repo <workspace/repository>` is required;
- `--url` is an explicit alias for the PR URL;
- `--output` accepts `table` or `json`;
- `--help` and `-h` display help.
- `jira-get-issue --issue` accepts either a Jira key or a complete `/browse/KEY-123` URL.
- the Jira command returns attachment metadata and content URLs; it does not download the binary files.

## Authentication notes

- each command uses its own token environment variable;
- for some flows, the CLI can reuse `BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN` to read PR metadata when the primary token does not have enough permission for that step;
- `bb-get-pr-sonar-issues` reads the Sonar report annotations from Bitbucket Code Insights with `BB_<WORKSPACE>_GET_PR_REPORTS_TOKEN`;
- `bb-get-pr-diff` requires a token with pull-request read access;
- commands that query the Sonar API directly use `SONAR_TOKEN` to read Sonar details;
- the pipeline command needs read access to Bitbucket Pipelines.
- the Jira command uses `JIRA_EMAIL` and `JIRA_API_TOKEN`; use a token with only the required Jira read scopes. The token must belong to a user with permission to browse the card and its restricted comments/attachments.

## Example with the published executable

After publishing, the same call looks like this:

```powershell
.\artifacts\atlascli\AtlasCli.exe bb-get-pr-comments --pr "https://bitbucket.org/acmeplatform/service-platform/pull-requests/684" --output json
```

## Current packaging status

The project is not packaged yet as a `dotnet tool`.

Right now, the recommended setup is:

1. use `dotnet run` during development;
2. use `dotnet publish` for local executable usage.
