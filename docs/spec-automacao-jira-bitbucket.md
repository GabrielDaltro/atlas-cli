# Spec Inicial da Suite de Scripts Jira e Bitbucket

## Objetivo

Definir uma primeira versao da interface de linha de comando para um conjunto de scripts que automatizam tarefas recorrentes no Jira, Bitbucket e Sonar.

Esta spec foca em contrato tecnico de uso:

- nome do script;
- finalidade;
- parametros obrigatorios e opcionais;
- formato esperado de saida;
- convencoes gerais da suite.

Nao define implementacao, linguagem nem altera regra de negocio existente.

## Escopo Inicial

### Jira

1. Criar um card
2. Ler um card retornando apenas titulo e status
3. Avancar um card para um estado
4. Baixar todos os cards de um board associados a uma pessoa
5. Adicionar comentario em um card
6. Baixar todos os cards vinculados a uma versao

### Bitbucket / CI / Sonar

1. Criar um PR
2. Ler os comentarios de um PR
3. Ler as tasks de um PR
4. Verificar PRs com build quebrado
5. Verificar por que um PR esta com build quebrado
6. Baixar comentarios dos PRs
7. Baixar analise do SonarCloud no PR

## Principios da Suite

- Cada automacao sera executada por linha de comando.
- Operacoes de escrita nao devem abrir prompt interativo para coletar dados de negocio.
- Toda informacao necessaria para escrita deve ser recebida por parametro.
- A suite deve ser amigavel para uso manual e para encadeamento em script.
- A saida padrao deve ser simples de ler no terminal, com opcao estruturada em JSON.
- Erros devem ser objetivos e sair em `stderr`.

## Estrutura Sugerida

Opcao inicial recomendada:

- um script por operacao;
- nomes padronizados por dominio;
- convencao de argumentos semelhante entre scripts.

Exemplo de nomes:

- `jira-create-card`
- `jira-get-card-summary`
- `jira-transition-card`
- `jira-list-board-cards`
- `jira-add-comment`
- `jira-list-version-cards`
- `bb-create-pr`
- `bb-get-pr-comments`
- `bb-get-pr-tasks`
- `bb-get-pr-reports`
- `bb-list-broken-pr-builds`
- `bb-explain-pr-build-failure`
- `bb-export-pr-comments`
- `sonar-get-pr-analysis`

## Convencoes Gerais

### Autenticacao

Credenciais devem vir preferencialmente por variaveis de ambiente.

Jira:

- `JIRA_BASE_URL`
- `JIRA_USER_EMAIL`
- `JIRA_API_TOKEN`

Bitbucket:

- `BITBUCKET_BASE_URL`
- `BITBUCKET_WORKSPACE`, quando o comando permitir workspace padrao
- `BITBUCKET_<WORKSPACE>_EMAIL`
- token especifico por script e workspace, por exemplo `BB_<WORKSPACE>_GET_PR_COMMENTS_TOKEN`

Observacao:

- scripts Bitbucket devem autenticar apenas com `email:token`;
- scripts dedicados nao devem aceitar `BITBUCKET_APP_PASSWORD` generico como fallback.
- o workspace deve ser extraido dos argumentos antes da leitura da credencial;
- cada script deve ler somente a credencial associada ao workspace do recurso solicitado.

Sonar:

- `SONAR_BASE_URL`
- `SONAR_TOKEN`

### Convencoes de Parametros

- Identificadores devem usar nomes explicitos, por exemplo `--issue`, `--board-id`, `--pr`, `--repo`.
- Campos textuais devem aceitar texto direto por parametro.
- Campos com multiplos valores devem aceitar repeticao do argumento ou lista separada por virgula.
- Quando houver ambiguidade entre nome e id, a spec deve explicitar o que o script espera.

### Convencoes de Saida

Todos os scripts devem suportar:

- `--output table` como padrao para uso humano;
- `--output json` para automacao;
- `--verbose` para detalhes tecnicos adicionais.

### Convencoes de Ajuda

- `--help` e `-h` devem mostrar ajuda sem exigir credenciais.
- Quando nenhum comando for informado, o CLI deve listar os comandos disponiveis.
- Cada comando deve ter ajuda propria com uso, parametros, variaveis de ambiente e exemplo.

### Convencoes de Erro

Codigos sugeridos:

- `0`: sucesso
- `1`: erro de validacao de parametros
- `2`: erro de autenticacao ou configuracao
- `3`: recurso nao encontrado
- `4`: operacao recusada por regra da integracao
- `5`: erro inesperado

## Scripts do Jira

### 1. `jira-create-card`

Cria um card no Jira.

Parametros obrigatorios:

- `--project <chave-do-projeto>`
- `--type <tipo-do-card>`
- `--title <titulo>`

Parametros opcionais:

- `--description <texto>`
- `--assignee <usuario>`
- `--priority <prioridade>`
- `--labels <label1,label2>`
- `--parent <issue-key>`
- `--board-id <id-do-board>`
- `--output <table|json>`

Saida esperada:

- chave do card criado;
- titulo;
- status inicial;
- URL do card.

Exemplo:

```bash
jira-create-card --project ABC --type Story --title "Ajustar validacao do fluxo"
```

### 2. `jira-get-card-summary`

Le um card retornando apenas titulo e status.

Parametros obrigatorios:

- `--issue <issue-key>`

Parametros opcionais:

- `--output <table|json>`

Saida esperada:

- `issue`
- `title`
- `status`

Exemplo:

```bash
jira-get-card-summary --issue ABC-123
```

### 3. `jira-transition-card`

Avanca um card para um estado informado.

Parametros obrigatorios:

- `--issue <issue-key>`
- `--to <nome-do-estado-destino>`

Parametros opcionais:

- `--comment <texto>`
- `--output <table|json>`

Saida esperada:

- issue;
- status anterior;
- status atual;
- transicao aplicada.

Observacao:

- o script deve falhar com mensagem clara quando a transicao nao estiver disponivel para o card atual.

Exemplo:

```bash
jira-transition-card --issue ABC-123 --to "Em Homologacao"
```

### 4. `jira-list-board-cards`

Lista todos os cards de um board associados a uma pessoa.

Parametros obrigatorios:

- `--board-id <id-do-board>`
- `--assignee <usuario>`

Parametros opcionais:

- `--status <status>`
- `--sprint <nome-ou-id>`
- `--output <table|json>`

Saida esperada:

Lista contendo, no minimo:

- issue;
- titulo;
- status;
- assignee.

Exemplo:

```bash
jira-list-board-cards --board-id 45 --assignee usuario@empresa.com
```

### 5. `jira-add-comment`

Adiciona comentario em um card.

Parametros obrigatorios:

- `--issue <issue-key>`
- `--comment <texto-do-comentario>`

Parametros opcionais:

- `--output <table|json>`

Saida esperada:

- issue;
- id do comentario;
- data de inclusao;
- URL do card.

Exemplo:

```bash
jira-add-comment --issue ABC-123 --comment "Validado em ambiente de teste."
```

### 6. `jira-list-version-cards`

Lista os cards vinculados a uma versao.

Parametros obrigatorios:

- `--project <chave-do-projeto>`
- `--version <nome-ou-id-da-versao>`

Parametros opcionais:

- `--status <status>`
- `--assignee <usuario>`
- `--output <table|json>`

Saida esperada:

Lista contendo, no minimo:

- issue;
- titulo;
- status;
- versao;
- assignee.

Exemplo:

```bash
jira-list-version-cards --project ABC --version 1.8.0
```

## Scripts do Bitbucket

### 7. `bb-create-pr`

Cria um pull request no Bitbucket.

Parametros obrigatorios:

- `--repo <workspace/repositorio>`
- `--title <titulo>`
- `--description <descricao>`
- `--target <branch-destino>`

Parametros opcionais:

- `--source <branch-origem>`
- `--reviewers <user1,user2>`
- `--draft`
- `--close-source-branch`
- `--output <table|json>`

Saida esperada:

- id do PR;
- titulo;
- branch de origem;
- branch de destino;
- URL do PR.

Observacao:

- na ausencia de `--source`, a implementacao pode assumir a branch atual do repositorio local, desde que isso fique documentado na ajuda do comando.

Exemplo:

```bash
bb-create-pr --repo workspace/atlascli --title "Refatora cliente Jira" --description "Organiza camada de integracao" --target develop
```

### 8. `bb-get-pr-comments`

Le comentarios de um PR especifico.

Parametros obrigatorios:

- identificacao do PR em uma das formas abaixo:
  - `--repo <workspace/repositorio>` com `--pr <numero-do-pr>`;
  - `--pr <url-do-pr>`;
  - `--url <url-do-pr>`.

Parametros opcionais:

- `--include-system`
- `--output <table|json>`

Observacoes:

- quando `--pr` receber apenas o numero do PR, `--repo` e obrigatorio;
- quando `--pr` receber uma URL, o script deve extrair workspace, repositorio e numero do PR;
- `--url` e um alias explicito para informar a URL do PR;
- as tres formas devem ser normalizadas internamente para o mesmo identificador: workspace, repositorio e numero do PR.

Saida esperada:

Lista contendo, no minimo:

- autor;
- data;
- comentario;
- arquivo ou contexto, quando aplicavel.

Exemplo:

```bash
bb-get-pr-comments --repo workspace/atlascli --pr 128
bb-get-pr-comments --pr "https://bitbucket.org/workspace/atlascli/pull-requests/128"
bb-get-pr-comments --url "https://bitbucket.org/workspace/atlascli/pull-requests/128"
```

### 9. `bb-get-pr-tasks`

Le tasks de um PR especifico.

Parametros obrigatorios:

- identificacao do PR em uma das formas abaixo:
  - `--repo <workspace/repositorio>` com `--pr <numero-do-pr>`;
  - `--pr <url-do-pr>`;
  - `--url <url-do-pr>`.

Parametros opcionais:

- `--output <table|json>`

Observacoes:

- quando `--pr` receber apenas o numero do PR, `--repo` e obrigatorio;
- quando `--pr` receber uma URL, o script deve extrair workspace, repositorio e numero do PR;
- `--url` e um alias explicito para informar a URL do PR;
- as tres formas devem ser normalizadas internamente para o mesmo identificador: workspace, repositorio e numero do PR.

Saida esperada:

Lista contendo, no minimo:

- id da task;
- autor;
- data;
- estado;
- texto da task;
- comentario associado, quando aplicavel.

Exemplo:

```bash
bb-get-pr-tasks --repo workspace/atlascli --pr 128
bb-get-pr-tasks --pr "https://bitbucket.org/workspace/atlascli/pull-requests/128"
bb-get-pr-tasks --url "https://bitbucket.org/workspace/atlascli/pull-requests/128"
```

### 10. `bb-get-pr-reports`

Le reports de Code Insights associados ao commit de origem de um PR.

Parametros obrigatorios:

- identificacao do PR em uma das formas abaixo:
  - `--repo <workspace/repositorio>` com `--pr <numero-do-pr>`;
  - `--pr <url-do-pr>`;
  - `--url <url-do-pr>`.

Parametros opcionais:

- `--output <table|json>`

Observacoes:

- quando `--pr` receber apenas o numero do PR, `--repo` e obrigatorio;
- quando `--pr` receber uma URL, o script deve extrair workspace, repositorio e numero do PR;
- o script deve ler os metadados do PR para descobrir o commit de origem;
- os reports devem ser obtidos pelo endpoint de Code Insights do Bitbucket;
- para reports do SonarCloud, o retorno esperado e o report publicado pelo Sonar no Bitbucket.

Saida esperada:

Lista contendo, no minimo:

- id do report;
- titulo;
- reporter;
- tipo;
- resultado;
- link;
- detalhes;
- dados estruturados do report.

Exemplo:

```bash
bb-get-pr-reports --repo workspace/atlascli --pr 128
bb-get-pr-reports --pr "https://bitbucket.org/workspace/atlascli/pull-requests/128"
bb-get-pr-reports --url "https://bitbucket.org/workspace/atlascli/pull-requests/128"
```

### 11. `bb-list-broken-pr-builds`

Lista PRs com build quebrado.

Parametros obrigatorios:

- `--repo <workspace/repositorio>`

Parametros opcionais:

- `--state <open|all>`
- `--author <usuario>`
- `--target <branch>`
- `--output <table|json>`

Saida esperada:

Lista contendo, no minimo:

- numero do PR;
- titulo;
- autor;
- status do build;
- pipeline ou build mais recente;
- URL do PR.

Exemplo:

```bash
bb-list-broken-pr-builds --repo workspace/atlascli
```

### 12. `bb-explain-pr-build-failure`

Explica por que um PR esta com build quebrado.

Parametros obrigatorios:

- `--repo <workspace/repositorio>`
- `--pr <numero-do-pr>`

Parametros opcionais:

- `--max-log-lines <n>`
- `--output <table|json>`

Saida esperada:

- status consolidado do build;
- pipeline inspecionado;
- etapa ou job com falha;
- motivo resumido da falha;
- trecho relevante do log, quando disponivel.

Exemplo:

```bash
bb-explain-pr-build-failure --repo workspace/atlascli --pr 128
```

### 13. `bb-export-pr-comments`

Baixa comentarios de varios PRs em lote.

Parametros obrigatorios:

- `--repo <workspace/repositorio>`

Parametros opcionais:

- `--state <open|merged|declined|all>`
- `--author <usuario>`
- `--target <branch>`
- `--from-date <yyyy-mm-dd>`
- `--to-date <yyyy-mm-dd>`
- `--output <table|json>`
- `--output-file <caminho>`

Saida esperada:

- colecao de PRs com seus comentarios;
- metadados minimos de cada PR;
- indicacao de quantidade total exportada.

Observacao:

- quando `--output-file` for informado, o script deve persistir o conteudo no arquivo e continuar emitindo um resumo curto no terminal.

Exemplo:

```bash
bb-export-pr-comments --repo workspace/atlascli --state open --output json
```

## Scripts do Sonar

### 14. `sonar-get-pr-analysis`

Baixa a analise do SonarCloud associada a um PR.

Parametros obrigatorios:

- `--project-key <chave-do-projeto-no-sonar>`
- `--pr <numero-do-pr>`

Parametros opcionais:

- `--repo <workspace/repositorio>`
- `--output <table|json>`

Saida esperada:

- status do quality gate;
- resumo de bugs;
- resumo de vulnerabilities;
- resumo de code smells;
- cobertura, quando disponivel;
- link para a analise.

Exemplo:

```bash
sonar-get-pr-analysis --project-key atlascli --pr 128
```

## Formato Minimo de JSON

Para facilitar automacao posterior, a resposta em JSON deve seguir um formato consistente.

Operacao de item unico:

```json
{
  "ok": true,
  "data": {}
}
```

Operacao de lista:

```json
{
  "ok": true,
  "count": 0,
  "data": []
}
```

Operacao com erro:

```json
{
  "ok": false,
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "Issue ABC-123 nao encontrado."
  }
}
```

## Requisitos Nao Funcionais

- Os scripts devem ser idempotentes quando a integracao permitir validacao segura antes da escrita.
- Logs tecnicos nao devem poluir a saida principal quando `--output json` for usado.
- Campos de texto devem preservar quebras de linha quando suportado pela integracao.
- A ajuda de cada comando deve conter pelo menos um exemplo real de uso.

## Pontos Para Validar na Proxima Iteracao

1. `card` no Jira significa sempre issue padrao ou precisamos tratar tipos especificos por projeto.
2. Para avancar estado, o destino sera informado por nome funcional do status ou por id da transicao.
3. Em `bb-create-pr`, a branch de origem deve ser obrigatoria ou pode assumir a branch local atual.
4. `Baixar comentarios dos PRs` sera uma exportacao em lote, como proposto, ou era a mesma necessidade de ler comentarios de um unico PR.
5. A analise mencionada como "sonar claud" foi interpretada como SonarCloud.
6. Precisamos definir se os scripts devem sempre operar por API remota ou se alguns podem aproveitar contexto do repositorio git local.

## Proximos Passos Sugeridos

1. Validar os pontos ambiguos desta spec.
2. Fechar o contrato final de nomes e parametros.
3. Escolher stack de implementacao.
4. Criar a estrutura base da suite e implementar os scripts do Jira primeiro.
