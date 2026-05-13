# Spec de Permissoes e Credenciais

## Objetivo

Definir a estrategia de credenciais da suite no cenario real deste projeto:

- sem acesso admin;
- sem criacao de `service account`;
- usando apenas o proprio usuario do desenvolvedor;
- com um token separado por script, ou no minimo por perfil de acesso;
- buscando o menor privilegio possivel dentro dessa limitacao.

## Cenario Real

Esta spec assume:

- Jira Cloud;
- Bitbucket Cloud;
- SonarQube Cloud ou SonarCloud;
- usuario desenvolvedor com acesso apenas a alguns boards, projetos e repositorios;
- nenhuma permissao administrativa para criar contas tecnicas, grant global ou tokens de repositorio/projeto administrados por terceiros.

## Consequencia de Seguranca

Como os scripts usarao o proprio usuario:

- toda acao feita pelos scripts aparecera em seu nome;
- os scripts nunca terao mais acesso do que o seu usuario ja possui;
- se o seu acesso aumentar no futuro, os scripts herdarao esse aumento;
- no Jira, o escopo do token limita a API, mas as permissoes reais continuam sendo as do seu usuario;
- no Bitbucket, o token de usuario herda o universo de repositorios aos quais seu usuario tem acesso, limitado pelos scopes escolhidos no token.

## Estrategia Recomendada

### Jira

Usar API tokens com scopes, criados na sua propria conta Atlassian.

Regras:

- um token por script quando o conjunto de scopes for diferente;
- usar a URL `https://api.atlassian.com/ex/jira/{cloudId}` para tokens com scopes;
- evitar token unico para todos os scripts;
- preferir endpoints e formatos de consulta que reduzam escopo necessario.

### Bitbucket

Usar API tokens da sua propria conta Bitbucket/Atlassian.

Regras:

- um token por script ou por perfil de acesso;
- limitar cada token aos scopes minimos;
- como voce nao e admin, nao assumir repository access token ou project access token;
- trabalhar com user API token como padrao.

### Sonar

Usar token proprio apenas para leitura do projeto necessario.

## Matriz Por Script

### 1. `jira-create-card`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-create-card-token`

Scopes:

- `write:issue:jira`
- `write:comment:jira`
- `write:comment.property:jira`
- `write:attachment:jira`
- `read:issue:jira`

Permissoes exigidas no Jira:

- Browse projects
- Create issues

Observacoes:

- os scopes acima sao os exigidos pela operacao de criacao de issue;
- mesmo que o script crie apenas titulo e descricao, o endpoint pede esse conjunto.

### 2. `jira-get-card-summary`

Objetivo funcional:

- ler apenas titulo e status

Implementacao recomendada:

- usar busca com campos limitados, por exemplo `fields=summary,status`, em vez de buscar o payload completo da issue

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-get-card-summary-token`

Scopes:

- `read:issue-details:jira`
- `read:audit-log:jira`
- `read:avatar:jira`
- `read:field-configuration:jira`
- `read:issue-meta:jira`

Permissoes exigidas no Jira:

- Browse projects
- permissao para ver a issue, se houver issue security

Observacoes:

- este token deve ser usado so para leitura de issue;
- nao incluir scopes de board, comment ou transicao.

### 3. `jira-transition-card`

Implementacao recomendada:

- consultar primeiro as transicoes disponiveis para a issue e so depois executar a escolhida

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-transition-card-token`

Scopes:

- `read:issue.transition:jira`
- `read:status:jira`
- `read:field-configuration:jira`
- `write:issue:jira`
- `write:issue.property:jira`

Permissoes exigidas no Jira:

- Browse projects
- permissao de executar a transicao no workflow do projeto

### 4. `jira-list-board-cards`

Implementacao recomendada:

- usar `board/{boardId}/issue` com filtro por `assignee`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-list-board-cards-token`

Scopes:

- `read:board-scope:jira-software`
- `read:issue-details:jira`

Permissoes exigidas no Jira:

- permissao para visualizar o board
- Browse projects nas issues retornadas

### 5. `jira-add-comment`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-add-comment-token`

Scope:

- `write:jira-work`

Permissoes exigidas no Jira:

- Browse projects
- Add comments

Observacoes:

- para esse script, o menor conjunto granular completo nao ficou claramente recuperavel na consulta publica usada nesta rodada;
- por isso, a opcao segura e pratica e um token dedicado apenas para comentario com `write:jira-work`;
- o risco continua controlado porque o token sera isolado por script.

### 6. `jira-list-version-cards`

Implementacao recomendada:

- usar JQL por `fixVersion`, por exemplo `project = ABC AND fixVersion = "1.8.0"`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `jira-list-version-cards-token`

Scopes:

- `read:issue-details:jira`
- `read:audit-log:jira`
- `read:avatar:jira`
- `read:field-configuration:jira`
- `read:issue-meta:jira`

Permissoes exigidas no Jira:

- Browse projects
- permissao para ver a issue, se houver issue security

Scope opcional:

- `read:project-version:jira`

Quando usar o scope opcional:

- somente se o script precisar validar a versao por API antes de executar a busca;
- se a busca usar diretamente o nome da versao na JQL, este scope pode ser evitado.

### 7. `bb-create-pr`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-create-pr-token`

Scopes:

- `write:pullrequest:bitbucket`

Scope opcional:

- `read:repository:bitbucket`

Quando o scope opcional e necessario:

- somente se o script consultar endpoints de repositorio para validar branch de origem ou destino antes de criar o PR

Observacoes:

- se o script apenas chama o endpoint de criacao de PR, o escopo de PR write basta;
- evitar validacao extra por API quando nao for essencial.

### 8. `bb-get-pr-comments`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-get-pr-comments-token`

Scopes:

- `read:pullrequest:bitbucket`

Observacoes:

- esse escopo permite visualizar PRs e comentarios;
- nao conceder acesso de escrita se o script so le.

### 9. `bb-get-pr-tasks`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-get-pr-tasks-token`

Scopes:

- `read:pullrequest:bitbucket`

Observacoes:

- esse escopo permite visualizar PRs e tasks;
- nao conceder acesso de escrita se o script so le;
- usar variavel segregada por workspace, por exemplo `BB_DYNAMOXTEAM_GET_PR_TASKS_TOKEN`.

### 10. `bb-get-pr-reports`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-get-pr-reports-token`

Scopes:

- `read:pullrequest:bitbucket`
- `read:repository:bitbucket`

Observacoes:

- `read:pullrequest:bitbucket` permite descobrir o commit de origem do PR;
- `read:repository:bitbucket` permite ler os reports de Code Insights do commit;
- se o token de reports nao tiver `read:pullrequest:bitbucket`, a implementacao pode usar o token segregado de leitura de comentarios do PR apenas para descobrir o commit de origem;
- nao conceder acesso de escrita se o script so le.

### 11. `bb-list-broken-pr-builds`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-list-broken-pr-builds-token`

Scopes:

- `read:pullrequest:bitbucket`
- `read:pipeline:bitbucket`

### 12. `bb-explain-pr-build-failure`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-explain-pr-build-failure-token`

Scopes:

- `read:pullrequest:bitbucket`
- `read:pipeline:bitbucket`

Observacoes:

- `read:pipeline:bitbucket` cobre pipeline, steps, logs, tests, artifacts e code insights;
- nao conceder `write:pipeline:bitbucket`, porque esse script nao deve rerodar nem disparar pipeline.

### 13. `bb-export-pr-comments`

Credencial:

- token do seu usuario

Nome sugerido do token:

- `bb-export-pr-comments-token`

Scopes:

- `read:pullrequest:bitbucket`

### 14. `sonar-get-pr-analysis`

Credencial:

- token do seu usuario no Sonar

Nome sugerido do token:

- `sonar-get-pr-analysis-token`

Permissao recomendada:

- Browse Project no projeto analisado

Observacoes:

- para projeto privado, esse acesso e obrigatorio;
- nao conceder permissoes administrativas nem de manutencao de issues se o script apenas le a analise.

## Lista Geral de Permissoes Necessarias

### Jira

Union dos scopes propostos:

- `write:issue:jira`
- `write:comment:jira`
- `write:comment.property:jira`
- `write:attachment:jira`
- `read:issue:jira`
- `read:issue-details:jira`
- `read:audit-log:jira`
- `read:avatar:jira`
- `read:field-configuration:jira`
- `read:issue-meta:jira`
- `read:issue.transition:jira`
- `read:status:jira`
- `read:board-scope:jira-software`
- `write:jira-work`

Scope opcional:

- `read:project-version:jira`

Permissoes de projeto no Jira:

- Browse projects
- Create issues
- Add comments
- permissao de transicionar issues no workflow
- permissao para visualizar os boards necessarios
- permissao para visualizar issues protegidas por issue security, quando aplicavel

### Bitbucket

Union dos scopes propostos:

- `read:pullrequest:bitbucket`
- `write:pullrequest:bitbucket`
- `read:pipeline:bitbucket`

Scope opcional:

- `read:repository:bitbucket`

### Sonar

- Browse Project

## Inventario Recomendado de Tokens

### Jira

1. `jira-create-card-token`
2. `jira-get-card-summary-token`
3. `jira-transition-card-token`
4. `jira-list-board-cards-token`
5. `jira-add-comment-token`
6. `jira-list-version-cards-token`

### Bitbucket

1. `bb-create-pr-token`
2. `bb-get-pr-comments-token`
3. `bb-get-pr-tasks-token`
4. `bb-get-pr-reports-token`
5. `bb-list-broken-pr-builds-token`
6. `bb-explain-pr-build-failure-token`
7. `bb-export-pr-comments-token`

### Sonar

1. `sonar-get-pr-analysis-token`

## Variaveis de Ambiente Sugeridas

### Jira

- `ATLASSIAN_EMAIL`
- `JIRA_CLOUD_ID`
- `JIRA_API_BASE_URL`
- `JIRA_CREATE_CARD_TOKEN`
- `JIRA_GET_CARD_SUMMARY_TOKEN`
- `JIRA_TRANSITION_CARD_TOKEN`
- `JIRA_LIST_BOARD_CARDS_TOKEN`
- `JIRA_ADD_COMMENT_TOKEN`
- `JIRA_LIST_VERSION_CARDS_TOKEN`

Observacao:

- `JIRA_API_BASE_URL` deve apontar para `https://api.atlassian.com/ex/jira/{cloudId}`

### Bitbucket

- `BITBUCKET_WORKSPACE`
- `BITBUCKET_BASE_URL`
- `BITBUCKET_<WORKSPACE>_EMAIL`
- `BB_<WORKSPACE>_CREATE_PR_TOKEN`
- `BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN`
- `BB_<WORKSPACE>_GET_PR_TASKS_TOKEN`
- `BB_<WORKSPACE>_GET_PR_REPORTS_TOKEN`
- `BB_<WORKSPACE>_LIST_BROKEN_PR_BUILDS_TOKEN`
- `BB_<WORKSPACE>_EXPLAIN_PR_BUILD_FAILURE_TOKEN`
- `BB_<WORKSPACE>_EXPORT_PR_COMMENTS_TOKEN`

Observacao:

- substituir `<WORKSPACE>` pelo workspace em maiusculo, trocando caracteres nao alfanumericos por `_`;
- exemplo para `dynamoxteam`: `BITBUCKET_DYNAMOXTEAM_EMAIL`, `BB_DYNAMOXTEAM_GET_PR_COMMENTS_TOKEN`, `BB_DYNAMOXTEAM_GET_PR_TASKS_TOKEN` e `BB_DYNAMOXTEAM_GET_PR_REPORTS_TOKEN`;
- nao usar token global para todos os workspaces, mesmo quando o email do usuario for o mesmo.

### Sonar

- `SONAR_BASE_URL`
- `SONAR_GET_PR_ANALYSIS_TOKEN`

## Decisoes de Implementacao Que Reduzem Permissao

1. `jira-get-card-summary` deve usar campos limitados em vez de payload completo de issue.
2. `jira-list-version-cards` deve usar JQL por `fixVersion` sempre que possivel.
3. `bb-create-pr` nao deve validar branch por endpoint de repositorio se nao houver necessidade concreta.
4. scripts de build quebrado devem ser estritamente read-only.
5. nenhum script deve compartilhar token com script de escrita quando isso puder ser evitado.

## Limites Deste Modelo

1. Este modelo nao entrega isolamento perfeito por recurso, porque o token representa o seu usuario.
2. Se voce ganhar acesso a novos projetos ou repositorios, os scripts passam a herdar esse acesso.
3. Se o Jira ou Bitbucket mudarem a matriz de scopes, a spec precisa ser revisitada.
4. Alguns endpoints do Jira podem exigir combinacoes granulares pouco intuitivas; durante a implementacao, qualquer divergencia encontrada deve ser registrada junto do endpoint afetado.

## Evolucao Futura

Se no futuro houver apoio administrativo, a evolucao recomendada e:

1. migrar Jira para `service accounts`;
2. migrar Bitbucket para repository access token ou project access token;
3. restringir tambem por ambiente e por time;
4. rotacionar e auditar tokens com periodicidade definida.

## Fontes Oficiais

- Jira API tokens com scopes: <https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/>
- Jira scopes: <https://developer.atlassian.com/cloud/jira/platform/scopes-for-oauth-2-3LO-and-forge-apps/>
- Jira Create issue: <https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/>
- Jira Issue comments: <https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-comments/>
- Jira Issue search: <https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search/>
- Jira Project versions: <https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-project-versions/>
- Jira Boards: <https://developer.atlassian.com/cloud/jira/software/rest/api-group-board/>
- Bitbucket API tokens: <https://support.atlassian.com/bitbucket-cloud/docs/api-tokens/>
- Bitbucket API token permissions: <https://support.atlassian.com/bitbucket-cloud/docs/api-token-permissions/>
- Bitbucket create API token: <https://support.atlassian.com/bitbucket-cloud/docs/create-an-api-token/>
- SonarQube Cloud tokens: <https://docs.sonarsource.com/sonarqube-cloud/managing-your-account/managing-tokens>
- SonarQube Cloud permissions: <https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/administering-your-projects/setting-permissions/>
