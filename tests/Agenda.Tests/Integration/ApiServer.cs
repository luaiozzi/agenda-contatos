using System.Net.Http.Headers;
using System.Net.Http.Json;
using Agenda.Api;
using Agenda.Api.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;

namespace Agenda.Tests.Integration;

/// <summary>
/// Sobe a API real (Kestrel, porta aleatória em 127.0.0.1) com um SQLite em memória exclusivo.
/// Usar um servidor real, em vez do TestServer, deixa os testes independentes da versão do runtime instalado.
/// </summary>
public sealed class ApiServer : IAsyncLifetime
{
    public const string AdminEmail = "admin@agenda.com";
    public const string AdminPassword = "Admin@123";

    private readonly string _connectionString =
        $"Data Source=agenda_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private SqliteConnection? _keepAlive;
    private WebApplication? _app;

    /// <summary>Cliente já autenticado como administrador.</summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>Cliente sem token.</summary>
    public HttpClient AnonymousClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Um banco SQLite em memória só existe enquanto houver ao menos uma conexão aberta.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        _app = AgendaApplication.Build(new[]
        {
            "--urls", "http://127.0.0.1:0",
            $"--ConnectionStrings:Default={_connectionString}",
            "--Jwt:Key=chave-de-testes-com-mais-de-32-caracteres-0123456789",
            "--Jwt:Issuer=AgendaApi.Tests",
            "--Jwt:Audience=AgendaClient.Tests",
            $"--Seed:AdminEmail={AdminEmail}",
            $"--Seed:AdminPassword={AdminPassword}"
        });

        await _app.StartAsync();

        var baseAddress = new Uri(_app.Urls.First());
        AnonymousClient = new HttpClient { BaseAddress = baseAddress };
        Client = new HttpClient { BaseAddress = baseAddress };

        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = AdminEmail, password = AdminPassword });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        AnonymousClient?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _keepAlive?.Dispose();
    }
}
