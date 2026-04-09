# Korp_Teste_Abner_Batista_Garbin

Sistema de emissão de Notas Fiscais desenvolvido como teste técnico para a Korp.

---

## Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Como Executar](#como-executar)
- [Funcionalidades](#funcionalidades)
- [Detalhamento Técnico](#detalhamento-técnico)

---

## Visão Geral

Aplicação fullstack composta por um frontend Angular e dois microsserviços .NET que se comunicam entre si, cada um com seu próprio banco de dados PostgreSQL. Todo o ambiente é orquestrado via Docker Compose.

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                    Docker Compose                        │
│                                                         │
│  ┌──────────────┐        ┌─────────────────────────┐   │
│  │   Frontend   │        │   FaturamentoService    │   │
│  │  Angular 19  │◄──────►│     ASP.NET Core 10     │   │
│  │  Nginx :4200 │        │         :5002           │   │
│  └──────────────┘        └────────────┬────────────┘   │
│                                       │ HTTP            │
│                          ┌────────────▼────────────┐   │
│                          │    EstoqueService        │   │
│                          │   ASP.NET Core 10        │   │
│                          │       :5001              │   │
│                          └────────────┬────────────┘   │
│                                       │                 │
│              ┌────────────────────────┴──────────┐     │
│  ┌───────────▼──────────┐  ┌────────────────────▼──┐  │
│  │     estoque-db        │  │     faturamento-db     │  │
│  │   PostgreSQL :5433    │  │   PostgreSQL :5434     │  │
│  └──────────────────────┘  └───────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

**EstoqueService** — gerencia produtos e saldos. Expõe endpoints REST para CRUD de produtos e operações de débito/crédito de saldo.

**FaturamentoService** — gerencia notas fiscais. Ao imprimir uma nota, chama o EstoqueService para debitar o saldo de cada produto. Em caso de falha parcial, executa compensação (rollback manual dos débitos já realizados).

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Frontend | Angular 19, Angular Material, RxJS |
| Backend | ASP.NET Core 10, Entity Framework Core, Npgsql |
| Banco de dados | PostgreSQL 16 |
| Infraestrutura | Docker, Docker Compose, Nginx |

---

## Como Executar

**Pré-requisitos:** Docker Desktop instalado e em execução.

```bash
git clone https://github.com/AbnerGarbin/Korp_Teste_Abner_Batista_Garbin.git
cd Korp_Teste_Abner_Batista_Garbin
docker-compose up --build
```

Aguarde todos os containers subirem. As migrations do banco de dados são aplicadas automaticamente.

| Serviço | URL |
|---|---|
| Frontend | http://localhost:4200 |
| EstoqueService (API) | http://localhost:5001 |
| FaturamentoService (API) | http://localhost:5002 |

---

## Funcionalidades

### Cadastro de Produtos
- Código, descrição e saldo inicial
- Listagem com edição e exclusão inline

### Cadastro de Notas Fiscais
- Numeração sequencial gerada automaticamente no backend (`Id.ToString("D5")`)
- Status inicial: **Aberta**
- Inclusão de múltiplos produtos com quantidade e preço unitário
- Edição e exclusão bloqueadas para notas **Fechadas**

### Impressão de Notas Fiscais
- Botão de impressão visível na listagem
- Spinner de processamento durante a operação
- Ao concluir: status atualizado para **Fechada** e saldo dos produtos debitado
- Notas com status diferente de **Aberta** não podem ser impressas

### Tratamento de Falhas
- Se o EstoqueService estiver indisponível, o FaturamentoService retorna erro 502 com mensagem amigável ao usuário
- Se um débito parcial falhar (ex: saldo insuficiente no segundo produto), os débitos já realizados são compensados via chamada ao endpoint `/creditar`, evitando inconsistência de dados
- Retry automático (2 tentativas) antes de propagar o erro

---

## Detalhamento Técnico

### Ciclos de vida do Angular utilizados

- **`ngOnInit`** — utilizado em todos os componentes de lista e formulário para carregar dados iniciais via serviço. Nos componentes de lista, 
o carregamento é disparado dentro de um `setTimeout` para garantir que a detecção de mudanças do Angular esteja estabilizada antes da primeira requisição HTTP.

- **`ngOnDestroy`** — utilizado em todos os componentes que possuem subscriptions RxJS. Emite um sinal em um `Subject` (`destroy$`) para cancelar todas 
as subscriptions ativas via operador `takeUntil`, evitando memory leaks.

### RxJS

Usado extensivamente nos serviços e componentes:

- **`HttpClient`** retorna `Observable` — todas as chamadas HTTP são tratadas como streams reativos
- **`catchError`** + **`throwError`** — intercepta erros HTTP nos serviços e os transforma em mensagens legíveis ao usuário, extraindo o campo `erro` do JSON de resposta do backend
- **`takeUntil(destroy$)`** — cancela subscriptions quando o componente é destruído
- **`finalize`** — garante que o indicador de carregamento (`imprimindo`, `salvando`) seja desativado independentemente de sucesso ou erro na operação de impressão

### Bibliotecas utilizadas

| Biblioteca | Finalidade |
|---|---|
| Angular Material | Componentes visuais: toolbar, sidenav, tabela, formulários, diálogos, chips, snackbar, spinner, ícones |
| Angular CDK | Base dos componentes do Material |
| RxJS | Programação reativa, gerenciamento de subscriptions |
| `@angular/forms` (ReactiveFormsModule) | Formulários reativos com `FormGroup`, `FormArray` e validadores |

### Componentes visuais

Todos os componentes visuais utilizam **Angular Material**. A configuração usa `provideAnimations()` (modo síncrono) para garantir renderização correta desde o primeiro carregamento.

### Frameworks utilizados no C#

- **ASP.NET Core 10** com controllers REST (`[ApiController]`, `[HttpGet]`, `[HttpPost]`, etc.)
- **Entity Framework Core** com provider **Npgsql** para PostgreSQL
- **`IHttpClientFactory`** para comunicação entre microsserviços (evita esgotamento de sockets)

### LINQ

Utilizado no Entity Framework Core para consultas ao banco de dados:

```csharp
// Include para carregar entidades relacionadas (eager loading)
await _db.NotasFiscais.Include(n => n.Itens).ToListAsync();

// FirstOrDefaultAsync com predicado lambda
await _db.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id);

// Any() para verificar falhas de compensação
if (compensationFailures.Any()) { ... }
```

### Tratamento de erros e exceções no backend

**EstoqueService:**
- Validações de negócio retornam `400 Bad Request` com objeto `{ "erro": "mensagem" }`
- Recurso não encontrado retorna `404 Not Found`
- `DbUpdateConcurrencyException` é capturada com retry (até 3 tentativas com backoff progressivo) para lidar com atualizações simultâneas de saldo; após esgotar as tentativas, retorna `409 Conflict`

**FaturamentoService:**
- `HttpRequestException` e `TaskCanceledException` são capturadas no helper `SendPatchWithRetryAsync` (2 tentativas); se o serviço de estoque permanecer inacessível, retorna `502 Bad Gateway`
- Falhas de débito parcial disparam compensação automática (`/creditar`) para reverter os débitos já aplicados
- Logs estruturados via `ILogger` em todos os pontos de erro para rastreabilidade

### Tratamento de Concorrência

Implementado no `EstoqueService` via **concorrência otimista** com `RowVersion` (coluna gerenciada pelo PostgreSQL). 
Ao detectar `DbUpdateConcurrencyException`, o contexto é reiniciado e a operação é refeita até 3 vezes com delay progressivo (100ms, 200ms, 300ms).

### Idempotência

A operação de impressão é idempotente: ao ser concluída com sucesso, um `ImpressaoToken` (GUID) é persistido na nota. 
Se a mesma requisição for repetida, o sistema detecta que a nota já possui token e retorna o resultado anterior sem reprocessar, evitando débitos duplicados.

### Gerenciamento de dependências

- **Frontend:** `npm` com `package.json` e `package-lock.json`
- **Backend:** NuGet gerenciado via arquivos `.csproj` com `<PackageReference>`; o `dotnet restore` é executado durante o build Docker
