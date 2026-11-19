# Agenda de Contatos

CRUD de agenda (nome, e-mail e telefone) com **.NET 8** no backend e **Vue 3 + PrimeVue** no frontend, protegido por **autenticação JWT**.

> **Login de demonstração:** `admin@agenda.com` / `Admin@123`
> (o usuário é criado automaticamente na primeira execução da API)

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Backend | .NET 8, ASP.NET Core Web API, Entity Framework Core 8 (SQLite), FluentValidation, autenticação JWT (JwtBearer), Swagger (Swashbuckle) |
| Testes backend | xUnit, Moq, FluentAssertions, SQLite em memória, servidor Kestrel real nos testes de integração |
| Frontend | Vue 3 (`<script setup>`), Vite, PrimeVue 4 (tema Aura), PrimeIcons |
| Infra | Docker, Docker Compose, nginx |

## Pré-requisitos

- [.NET SDK 8.0 ou superior](https://dotnet.microsoft.com/download)
- [Node.js 20 ou superior](https://nodejs.org/) (inclui o npm)
- Opcional: [Docker](https://www.docker.com/products/docker-desktop/), para a execução com contêineres

Para conferir: `dotnet --version` e `node --version`.

O projeto compila para .NET 8 e também funciona com SDKs/runtimes mais novos (9, 10). No Windows, use o PowerShell ou o Prompt de Comando; depois de instalar qualquer ferramenta (.NET, Node, Docker, Git), **feche e abra o terminal de novo** para ele reconhecer o comando.

## Acesso (usuário de demonstração)

Na primeira execução a API cria um usuário administrador:

| E-mail | Senha |
| --- | --- |
| `admin@agenda.com` | `Admin@123` |

Os valores vêm da seção `Seed` do `src/Agenda.Api/appsettings.json` (e podem ser trocados por variáveis de ambiente). Esse usuário é apenas para demonstração.

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
- O banco SQLite (`agenda.db`) e o usuário administrador são criados automaticamente na primeira execução, dentro da pasta `src/Agenda.Api`.
- Se você já rodou uma versão anterior do projeto (sem autenticação), o `agenda.db` antigo não tem a tabela de usuários e a API não inicia, mostrando uma mensagem pedindo para apagá-lo. Apague o arquivo e inicie de novo:

  ```powershell
  # PowerShell (Windows)
  Remove-Item .\src\Agenda.Api\agenda.db* -Force
  ```

  ```bash
  # Linux / macOS / Git Bash
  rm -f src/Agenda.Api/agenda.db*
  ```

### 2. Testes do backend

```bash
dotnet test Agenda.sln
```

Todos os testes devem passar (unitários, de repositório e de integração; os de integração sobem a API de verdade, em memória, e não precisam da API rodando). Para ver a saída detalhada, inclusive as mensagens de erro de cada falha, rode `dotnet test Agenda.sln --tl:off`. Evite rodar os testes com a API aberta em outro terminal: o arquivo compilado fica travado.

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

Acesse <http://localhost:5173> e entre com `admin@agenda.com` / `Admin@123`. A sessão dura 60 minutos; depois disso, a aplicação volta para a tela de login. Em desenvolvimento, o Vite encaminha as chamadas `/api/*` para `http://localhost:5080` (veja `frontend/vite.config.js`).

Build de produção do frontend: `npm run build` (saída em `frontend/dist`).

> No PowerShell, se aparecer o erro "a execução de scripts foi desabilitada neste sistema", rode uma vez `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned` ou use `npm.cmd install` / `npm.cmd run dev`.

## Rodando com Docker (alternativa)

Sobe API e frontend juntos, sem precisar instalar .NET nem Node. Requer Docker com Docker Compose.

```bash
docker compose up --build
```

- Aplicação: <http://localhost:8080>
- Swagger: <http://localhost:5080/swagger>

Para parar: `Ctrl+C` (ou `docker compose down` em outro terminal). Os dados ficam no volume `agenda-data` e são mantidos entre execuções; para apagá-los, use `docker compose down -v`.

Opcionalmente, copie `.env.example` para `.env` para trocar o segredo do JWT (`JWT_KEY`) e o usuário inicial (`ADMIN_EMAIL`, `ADMIN_PASSWORD`). Sem o `.env`, valores de desenvolvimento são usados.

| Arquivo | Função |
| --- | --- |
| `src/Agenda.Api/Dockerfile` | Build multi-stage da API (SDK 8.0 -> runtime ASP.NET 8.0, usuário sem privilégios) |
| `frontend/Dockerfile` | Build do Vue com Node 20 e entrega dos arquivos estáticos com nginx |
| `frontend/nginx.conf` | Serve o frontend e encaminha `/api/*` para a API |
| `docker-compose.yml` | Orquestra os dois serviços, o volume do banco SQLite e as variáveis de ambiente |

## Autenticação

- `POST /api/auth/login` recebe `{ "email": "...", "password": "..." }` e devolve `{ "token": "...", "expiresAt": "...", "email": "..." }`.
- Todos os endpoints de `/api/contacts` exigem o cabeçalho `Authorization: Bearer <token>`. Sem token, ou com token inválido/expirado, a API responde **401**.
- **Swagger**: faça o login em `POST /api/auth/login`, copie o valor de `token`, clique em **Authorize** e cole o token (somente o token, sem a palavra `Bearer`).
- O token é um JWT assinado com HMAC-SHA256, com validade de 60 minutos (configurável). As senhas são guardadas apenas como hash (PBKDF2, via `PasswordHasher` do ASP.NET Core Identity).
- E-mail desconhecido e senha errada retornam a mesma mensagem ("E-mail ou senha inválidos."), para não revelar quais e-mails existem.
- No frontend, o token fica no `localStorage`; se a API responder 401, o usuário volta automaticamente para a tela de login.

### Configuração

| Chave (`appsettings.json`) | Variável de ambiente | Descrição |
| --- | --- | --- |
| `Jwt:Key` | `Jwt__Key` | Segredo de assinatura (mínimo de 32 caracteres; a API não inicia sem ele) |
| `Jwt:Issuer` / `Jwt:Audience` | `Jwt__Issuer` / `Jwt__Audience` | Emissor e público do token |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | Validade do token, em minutos |
| `Seed:AdminEmail` / `Seed:AdminPassword` | `Seed__AdminEmail` / `Seed__AdminPassword` | Usuário criado na primeira execução (se vazio, nenhum é criado) |

A chave que está no `appsettings.json` é **apenas de desenvolvimento**. Em produção, ela deve vir de um segredo (variável de ambiente, cofre de segredos), nunca do repositório.

## Endpoints

| Método | Rota | Autenticação | Descrição | Respostas |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/login` | Não | Autentica e devolve o token JWT | 200, 400, 401 |
| GET | `/api/contacts?search=&page=1&pageSize=10` | Sim | Lista paginada, com busca por nome, e-mail ou telefone | 200, 401 |
| GET | `/api/contacts/{id}` | Sim | Busca um contato | 200, 401, 404 |
| POST | `/api/contacts` | Sim | Cria um contato | 201, 400, 401, 409 |
| PUT | `/api/contacts/{id}` | Sim | Atualiza um contato | 200, 400, 401, 404, 409 |
| DELETE | `/api/contacts/{id}` | Sim | Exclui um contato | 204, 401, 404 |

Exemplo de corpo (POST/PUT de contatos):

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
5. **Login**: e-mail e senha obrigatórios; o e-mail não diferencia maiúsculas de minúsculas.
6. O frontend valida os mesmos campos antes de enviar e exibe os erros retornados pela API.

## Arquitetura

```
Agenda.sln
├── src/Agenda.Api
│   ├── AgendaApplication.cs -> montagem da aplicação (serviços, JWT, Swagger, pipeline)
│   ├── Controllers/   -> AuthController, ContactsController (HTTP, sem regra de negócio)
│   ├── Services/      -> ContactService, AuthService, JwtTokenService (regras de negócio) e suas interfaces
│   ├── Repositories/  -> ContactRepository, UserRepository (acesso a dados com EF Core) e suas interfaces
│   ├── Data/          -> AgendaDbContext, DbSeeder (usuário inicial)
│   ├── Models/        -> Contact, AppUser (entidades), PagedResult<T>
│   ├── Dtos/          -> ContactRequest, ContactDto, LoginRequest, LoginResponse
│   ├── Validators/    -> ContactRequestValidator, LoginRequestValidator (FluentValidation)
│   ├── Mappings/      -> ContactMappings (conversões entidade <-> DTO)
│   ├── Settings/      -> JwtSettings, SeedSettings (configuração tipada)
│   ├── Middleware/    -> ExceptionHandlingMiddleware (exceções -> ProblemDetails)
│   ├── Helpers/       -> PhoneNormalizer
│   └── Exceptions/    -> NotFoundException, ConflictException, InvalidCredentialsException
├── tests/Agenda.Tests
│   ├── Helpers/, Validators/, Mappings/   -> testes unitários
│   ├── Services/                          -> regras de negócio (ContactService, AuthService, JwtTokenService), repositório mockado (Moq)
│   ├── Repositories/                      -> EF Core com SQLite em memória (contatos, usuários, usuário inicial)
│   └── Integration/                       -> API completa em Kestrel (porta aleatória, SQLite em memória), incluindo login e 401
└── frontend/src
    ├── components/    -> ContactTable, ContactFormDialog, SearchBox
    ├── views/         -> ContactsView, LoginView
    ├── composables/   -> useAuth (estado da sessão)
    ├── services/      -> api.js (fetch, token e tratamento de erro), contactService.js, authService.js, session.js
    └── utils/         -> phone.js, validation.js
```

Padrões e decisões:

- **Camadas com interfaces e injeção de dependência**: controller -> service -> repository, tudo registrado em `AgendaApplication.Build`.
- **Tratamento de erros centralizado**: services lançam exceções de domínio (`NotFoundException`, `ConflictException`, `InvalidCredentialsException`, `ValidationException`); o middleware converte em 404, 409, 401 e 400.
- **SQLite + `EnsureCreated()`**: escolhido para o projeto rodar sem instalar banco. Em produção, o recomendado seria um banco servidor (PostgreSQL / SQL Server) com migrations do EF Core.
- **Validação com FluentValidation no service** (em vez de auto-validação do MVC): mantém um único formato de resposta de erro e deixa a regra testável sem HTTP.
- **Testes de integração com Kestrel real**: a montagem da aplicação fica em `AgendaApplication.Build`, e os testes sobem a API de verdade em uma porta aleatória. Assim eles não dependem da versão do runtime do .NET instalada (o `TestServer` do ASP.NET precisa ter a mesma versão do runtime).
- **Mapeamento manual (sem AutoMapper)**: com poucos campos, métodos de extensão explícitos (`ContactMappings`) são mais simples e evitam uma dependência. As versões do AutoMapper disponíveis na criação do projeto tinham avisos de vulnerabilidade (NU1903) ou exigem chave de licença.
- **Frontend com componentes de responsabilidade única**: a view cuida do estado e das chamadas; a tabela, o formulário e a busca são componentes reutilizáveis e independentes da API.

## Possíveis evoluções

CQRS, mensageria com RabbitMQ, testes do frontend (Vitest), migrations do EF Core, refresh token e cadastro/gestão de usuários.

## Solução de problemas

- **Erro ao iniciar: "O banco de dados existente foi criado por uma versão anterior..."**: o arquivo `agenda.db` é de uma versão sem autenticação. Pare a API, apague `src/Agenda.Api/agenda.db` e inicie de novo. No Docker, use `docker compose down -v` e suba novamente.
- **Erro "Jwt:Key ... pelo menos 32 caracteres"**: a chave do JWT está vazia ou curta demais; ajuste `Jwt:Key` (ou `JWT_KEY` no Docker).
- **Porta 5080 ou 5173 em uso**: troque a porta em `--urls` (backend) e em `frontend/vite.config.js` (proxy e `server.port`). Se mudar a porta do front, ajuste também a política de CORS em `src/Agenda.Api/AgendaApplication.cs`.
- **`npm install` falha por versão do Node**: use Node 20 ou superior.
- **Tela do frontend sem dados / erro de conexão**: confirme que a API está rodando em <http://localhost:5080/swagger>.
- **Recriar o banco**: pare a API e apague `src/Agenda.Api/agenda.db` (comando no passo 1 do passo a passo).
- **Login diz "E-mail ou senha inválidos"**: confira se digitou `admin@agenda.com` / `Admin@123` exatamente assim (a senha diferencia maiúsculas). O usuário só é criado quando o banco é novo e a API sobe sem erros; se você mudou `Seed:AdminEmail` / `Seed:AdminPassword` depois da primeira execução, a senha do usuário que já existe **não** é alterada. Apague o `agenda.db` para recriá-lo.
- **`docker` ou `git` "não é reconhecido como nome de cmdlet"**: a ferramenta não está instalada ou o terminal foi aberto antes da instalação. Instale e abra um terminal novo.
- **Docker: a API não sobe e o log fala em banco existente**: o volume guarda um banco de uma versão anterior. Rode `docker compose down -v` e suba de novo.

## Processo de desenvolvimento

Projeto desenvolvido com apoio de um agente de IA (Claude): arquitetura, código e testes foram gerados a partir dos requisitos do teste e revisados por mim.
