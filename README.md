# Agenda de Contatos

CRUD de agenda (nome, e-mail e telefone) com **.NET 8** no backend e **Vue 3 + PrimeVue** no frontend.

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Backend | .NET 8, ASP.NET Core Web API, Entity Framework Core 8 (SQLite), FluentValidation, Swagger (Swashbuckle) |
| Testes backend | xUnit, Moq, FluentAssertions, SQLite em memória, servidor Kestrel real nos testes de integração |
| Frontend | Vue 3 (`<script setup>`), Vite, PrimeVue 4 (tema Aura), PrimeIcons |

## Pré-requisitos

- [.NET SDK 8.0 ou superior](https://dotnet.microsoft.com/download)
- [Node.js 20 ou superior](https://nodejs.org/) (inclui o npm)

Para conferir: `dotnet --version` e `node --version`.

## Passo a passo para rodar

Execute os comandos a partir da raiz do projeto (pasta que contém `Agenda.sln`).

### 1. Backend

```bash
# Restaurar dependências e compilar
dotnet restore Agenda.sln
dotnet build Agenda.sln --no-restore

# Subir a API em http://localhost:5080
dotnet run --project src/Agenda.Api --urls http://localhost:5080
```

- Swagger: <http://localhost:5080/swagger>
- O banco SQLite (`agenda.db`) é criado automaticamente na primeira execução, dentro da pasta `src/Agenda.Api`.

### 2. Testes do backend

```bash
dotnet test Agenda.sln
```

Para gerar cobertura de código (coverlet):

```bash
dotnet test Agenda.sln --collect:"XPlat Code Coverage"
```

### 3. Frontend

Em **outro terminal**, com a API rodando:

```bash
cd frontend
npm install
npm run dev
```

Acesse <http://localhost:5173>. Em desenvolvimento, o Vite encaminha as chamadas `/api/*` para `http://localhost:5080` (veja `frontend/vite.config.js`).

Build de produção do frontend: `npm run build` (saída em `frontend/dist`).

## Rodando com Docker (alternativa)

Sobe API e frontend juntos, sem precisar instalar .NET nem Node. Requer [Docker](https://www.docker.com/products/docker-desktop/) com Docker Compose.

```bash
docker compose up --build
```

- Aplicação: <http://localhost:8080>
- Swagger: <http://localhost:5080/swagger>

Para parar: `Ctrl+C` (ou `docker compose down` em outro terminal). Os dados ficam no volume `agenda-data` e são mantidos entre execuções; para apagá-los, use `docker compose down -v`.

| Arquivo | Função |
| --- | --- |
| `src/Agenda.Api/Dockerfile` | Build multi-stage da API (SDK 8.0 -> runtime ASP.NET 8.0, usuário sem privilégios) |
| `frontend/Dockerfile` | Build do Vue com Node 20 e entrega dos arquivos estáticos com nginx |
| `frontend/nginx.conf` | Serve o frontend e encaminha `/api/*` para a API |
| `docker-compose.yml` | Orquestra os dois serviços e o volume do banco SQLite |

## Endpoints

| Método | Rota | Descrição | Respostas |
| --- | --- | --- | --- |
| GET | `/api/contacts?search=&page=1&pageSize=10` | Lista paginada, com busca por nome, e-mail ou telefone | 200 |
| GET | `/api/contacts/{id}` | Busca um contato | 200, 404 |
| POST | `/api/contacts` | Cria um contato | 201, 400, 409 |
| PUT | `/api/contacts/{id}` | Atualiza um contato | 200, 400, 404, 409 |
| DELETE | `/api/contacts/{id}` | Exclui um contato | 204, 404 |

Exemplo de corpo (POST/PUT):

```json
{
  "name": "Ana Souza",
  "email": "ana.souza@example.com",
  "phone": "(19) 99999-8888"
}
```

Erros seguem o padrão ProblemDetails (RFC 7807). Em validações (400), o campo `errors` traz as mensagens por campo.

## Regras de negócio e validações

1. **Nome**: obrigatório, de 2 a 100 caracteres (após remover espaços nas pontas).
2. **E-mail**: obrigatório, formato válido, até 150 caracteres. É normalizado (sem espaços, em minúsculas) e **único** na agenda. Duplicidade retorna **409 Conflict**. A unicidade também é garantida por índice único no banco.
3. **Telefone** (padrão brasileiro): obrigatório; aceita máscara e `+55`. É armazenado **somente com dígitos** (DDD + número).
   - 11 dígitos: celular, o número começa com 9.
   - 10 dígitos: fixo, o número começa com 2 a 5.
   - DDD de dois dígitos, sem zero à esquerda e sem terminar em zero.
4. **Listagem**: ordenada por nome, paginada (máximo de 100 itens por página) e com busca por nome, e-mail ou telefone.
5. O frontend valida os mesmos campos antes de enviar e exibe os erros retornados pela API.

## Arquitetura

```
Agenda.sln
├── src/Agenda.Api
│   ├── Controllers/   -> ContactsController (HTTP, sem regra de negócio)
│   ├── Services/      -> IContactService / ContactService (regras de negócio)
│   ├── Repositories/  -> IContactRepository / ContactRepository (acesso a dados com EF Core)
│   ├── Data/          -> AgendaDbContext
│   ├── Models/        -> Contact (entidade), PagedResult<T>
│   ├── Dtos/          -> ContactRequest (entrada), ContactDto (saída)
│   ├── Validators/    -> ContactRequestValidator (FluentValidation)
│   ├── Mappings/      -> ContactMappings (conversões entidade <-> DTO)
│   ├── Middleware/    -> ExceptionHandlingMiddleware (exceções -> ProblemDetails)
│   ├── Helpers/       -> PhoneNormalizer
│   └── Exceptions/    -> NotFoundException, ConflictException
├── tests/Agenda.Tests
│   ├── Helpers/, Validators/, Mappings/   -> testes unitários
│   ├── Services/                          -> regras de negócio, repositório mockado (Moq)
│   ├── Repositories/                      -> EF Core com SQLite em memória
│   └── Integration/                       -> API completa em Kestrel (porta aleatória, SQLite em memória)
└── frontend/src
    ├── components/    -> ContactTable, ContactFormDialog, SearchBox
    ├── views/         -> ContactsView (orquestra os componentes)
    ├── services/      -> api.js (fetch + tratamento de erro), contactService.js
    └── utils/         -> phone.js, validation.js
```

Padrões e decisões:

- **Camadas com interfaces e injeção de dependência**: controller -> service -> repository, tudo registrado no `Program.cs`.
- **Tratamento de erros centralizado**: services lançam exceções de domínio (`NotFoundException`, `ConflictException`, `ValidationException`); o middleware converte em 404, 409 e 400.
- **SQLite + `EnsureCreated()`**: escolhido para o projeto rodar sem instalar banco. Em produção, o recomendado seria um banco servidor (PostgreSQL / SQL Server) com migrations do EF Core.
- **Validação manual com FluentValidation no service** (em vez de auto-validação do MVC): mantém um único formato de resposta de erro e deixa a regra testável sem HTTP.
- **Testes de integração com Kestrel real**: a montagem da aplicação fica em `AgendaApplication.Build`, e os testes sobem a API de verdade em uma porta aleatória. Assim eles não dependem da versão do runtime do .NET instalada (o `TestServer` do ASP.NET precisa ter a mesma versão do runtime).
- **Mapeamento manual (sem AutoMapper)**: com três campos, métodos de extensão explícitos (`ContactMappings`) são mais simples e evitam uma dependência. As versões do AutoMapper disponíveis na criação do projeto tinham avisos de vulnerabilidade (NU1903) ou exigem chave de licença.
- **Frontend com componentes de responsabilidade única**: a view cuida do estado e das chamadas; a tabela, o formulário e a busca são componentes reutilizáveis e independentes da API.

## Possíveis evoluções

CQRS (MediatR), autenticação JWT, mensageria com RabbitMQ, testes do frontend (Vitest) e migrations do EF Core.

## Solução de problemas

- **Porta 5080 ou 5173 em uso**: troque a porta em `--urls` (backend) e em `frontend/vite.config.js` (proxy e `server.port`). Se mudar a porta do front, ajuste também a política de CORS em `src/Agenda.Api/Program.cs`.
- **`npm install` falha por versão do Node**: use Node 20 ou superior.
- **Tela do frontend sem dados / erro de conexão**: confirme que a API está rodando em <http://localhost:5080/swagger>.
- **Recriar o banco**: pare a API e apague `src/Agenda.Api/agenda.db`.

## Processo de desenvolvimento

Projeto desenvolvido com apoio de um agente de IA (Claude): arquitetura, código e testes foram gerados a partir dos requisitos do teste e revisados por mim.
