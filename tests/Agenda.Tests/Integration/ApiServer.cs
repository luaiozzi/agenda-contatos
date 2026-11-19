using Agenda.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;

namespace Agenda.Tests.Integration;

/// <summary>
/// Sobe a API real (Kestrel, porta aleatória em 127.0.0.1) com um SQLite em memória exclusivo.
/// Usar um servidor real, em vez do TestServer, deixa os testes independentes da versão do runtime instalado.
/// </summary>
public sealed class ApiServer : IAsyncLifetime
{
    private readonly string _connectionString =
        $"Data Source=agenda_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private SqliteConnection? _keepAlive;
    private WebApplication? _app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Um banco SQLite em memória só existe enquanto houver ao menos uma conexão aberta.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        _app = AgendaApplication.Build(new[]
        {
            "--urls", "http://127.0.0.1:0",
            $"--ConnectionStrings:Default={_connectionString}"
        });

        await _app.StartAsync();

        Client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _keepAlive?.Dispose();
    }
}
