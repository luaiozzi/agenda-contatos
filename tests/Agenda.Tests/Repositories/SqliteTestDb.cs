using Agenda.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Tests.Repositories;

/// <summary>
/// Banco SQLite em memória, vivo enquanto a conexão estiver aberta.
/// Cada instância é um banco novo e isolado.
/// </summary>
public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AgendaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AgendaDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AgendaDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
