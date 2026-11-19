using Agenda.Api.Models;
using Agenda.Api.Repositories;
using FluentAssertions;

namespace Agenda.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteTestDb _db = new();

    public void Dispose() => _db.Dispose();

    private async Task SeedAsync(string email)
    {
        using var context = _db.CreateContext();
        context.Users.Add(new AppUser { Email = email, PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenExists_ReturnsUser()
    {
        await SeedAsync("ana@example.com");

        using var context = _db.CreateContext();
        var repository = new UserRepository(context);

        var user = await repository.GetByEmailAsync("ana@example.com", CancellationToken.None);

        user.Should().NotBeNull();
        user!.Email.Should().Be("ana@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenMissing_ReturnsNull()
    {
        await SeedAsync("ana@example.com");

        using var context = _db.CreateContext();
        var repository = new UserRepository(context);

        var user = await repository.GetByEmailAsync("outra@example.com", CancellationToken.None);

        user.Should().BeNull();
    }
}
