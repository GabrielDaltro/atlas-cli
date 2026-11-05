---
name: atlas-cli
description: Usa a AtlasCli para consultar cards e listar cards de um board por responsável no Jira Cloud, criar cards em sprints do Jira quando solicitado explicitamente e consultar pull requests ou logs de pipeline no Bitbucket Cloud. Use para operar sobre dados reais nesses serviços. Não use para orientação conceitual ou para outras alterações no Jira ou no Bitbucket.
---

# Atlas CLI

Use a CLI deste repositório. Faça as consultas e criações pelos comandos documentados, sem improvisar chamadas HTTP.

## Executar a CLI

Execute a partir da raiz do repositório, dois níveis acima desta pasta. Use `--output json` para interpretar o resultado e `<comando> --help` para conferir os argumentos e as credenciais esperadas.

```powershell
dotnet run --project .\src\AtlasCli.Cli\AtlasCli.Cli.csproj -- <comando> --output json
```

Peça as referências obrigatórias que estiverem faltando antes de executar o comando.

## Jira Cloud

Para listar cards de um board ou criar um card em uma sprint, confira se `jira --help` lista o comando na versão local da CLI.

- Use `jira-get-issue --issue "<chave-ou-url>"` para consultar campos, comentários e metadados de anexos. O comando aceita uma chave como `DYN-123` ou a URL completa do card em `/browse/`. Use `JIRA_API_TOKEN`.
- Use `jira-list-board-cards --board-id <id> --assignee "<usuario>"` para listar os cards atribuídos a um usuário. O responsável pode ser nome completo, account ID, e-mail ou `currentUser()`. Acrescente `--status "<status>"` ou `--sprint "<nome-ou-id>"` conforme os filtros solicitados. Use `JIRA_BOARD_API_TOKEN`.
- Execute `jira-create-card --board-id <id> --sprint-id <id> --project <chave> --issue-type "<nome-ou-id>" --summary "<titulo>"` somente quando o usuário pedir explicitamente a criação. A descrição é opcional e usa `--description "<texto>"`. Use IDs numéricos positivos para o board e a sprint, que deve estar ativa ou futura no board. Use `JIRA_CREATE_CARD_API_TOKEN` e informe a chave e a URL retornadas. Se o resultado da criação for incerto, verifique se o card já existe antes de repetir o comando.

Todos os comandos Jira usam `JIRA_EMAIL` e `JIRA_BASE_URL`. A URL base é dispensável quando `jira-get-issue` recebe a URL completa do card.

## Bitbucket Cloud

Para os comandos de PR, use `--pr "<url-do-pr>"` ou `--repo "<workspace/repository>" --pr <numero>`.

| Dados | Comando |
| --- | --- |
| Comentários | `bb-get-pr-comments` |
| Tarefas | `bb-get-pr-tasks` |
| Relatórios Code Insights | `bb-get-pr-reports` |
| Linhas sem cobertura no Sonar | `bb-get-pr-uncovered-lines` |
| Issues Sonar nas anotações de Code Insights | `bb-get-pr-sonar-issues` |
| Branches de origem e destino | `bb-get-pr-branches` |
| Diff unificado | `bb-get-pr-diff` |
| Log de pipeline | `bb-get-pr-pipeline-log` |

Para incluir comentários do sistema, acrescente `--include-system`. A consulta de linhas sem cobertura acessa o Sonar diretamente e usa `SONAR_TOKEN` para projetos privados.

O comando de log usa por padrão o pipeline referenciado no PR. Acrescente `--latest-commit-pipeline` para buscar o pipeline mais recente do commit de origem ou use `--repo "<workspace/repository>" --build <numero>` para buscar um build específico.

## Tratar resultados

- Resuma apenas os campos pertinentes à pergunta e sinalize dados ausentes, acesso negado ou referência inexistente.
- Não exponha tokens, valores de variáveis de ambiente ou informações que não tenham relação com a solicitação.
- Se a CLI informar credenciais ausentes, indique os nomes das variáveis conforme a mensagem, a ajuda do comando ou o [README](../../README.md). Oriente o usuário a configurar as credenciais no ambiente.
