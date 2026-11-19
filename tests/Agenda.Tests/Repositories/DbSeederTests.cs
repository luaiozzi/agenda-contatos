using Agenda.Api.Data;
using Agenda.Api.Models;
using Agenda.Api.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Tests.Repositories;

public class DbSeederTests
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    [Fact]
    public void SeedAdmin_CreatesAdminWithNormalizedEmailAndHashedPassword()
    {
        using var db = new SqliteTestDb();
        using var context = db.CreateContext();

        DbSeeder.SeedAdmin(
            context,
            _hasher,
            new SeedSettings { AdminEmail = " Admin@Agenda.com ", AdminPassword = "Admin@123" });

        var user = context.Users.Single();
        user.Email.Should().Be("admin@agenda.com");
        user.PasswordHash.Should().NotBe("Admin@123");
        _hasher.VerifyHashedPassword(user, user.PasswordHash, "Admin@123")
            .Should().NotBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void SeedAdmin_WhenCalledTwice_DoesNotDuplicateOrChangeTheUser()
    {
        using var db = new SqliteTestDb();
        using var context = db.CreateContext();

        DbSeeder.SeedAdmin(
            context,
            _hasher,
            new SeedSettings { AdminEmail = "admin@agenda.com", AdminPassword = "Admin@123" });
        var firstHash = context.Users.Single().PasswordHash;

        DbSeeder.SeedAdmin(
            context,
            _hasher,
            new SeedSettings { AdminEmail = "admin@agenda.com", AdminPassword = "OutraSenha@456" });

        context.Users.Count().Should().Be(1);
        context.Users.Single().PasswordHash.Should().Be(firstHash);
    }

    [Theory]
    [InlineData("", "Admin@123")]
    [InlineData("admin@agenda.com", "")]
    public void SeedAdmin_WithIncompleteSettings_CreatesNothing(string email, string password)
    {
        using var db = new SqliteTestDb();
        using var context = db.CreateContext();

        DbSeeder.SeedAdmin(
            context,
            _hasher,
            new SeedSettings { AdminEmail = email, AdminPassword = password });

        context.Users.Count().Should().Be(0);
    }

    [Fact]
    public void SeedAdmin_WhenDatabaseHasNoUsersTable_ThrowsWithClearInstructions()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AgendaDbContext>().UseSqlite(connection).Options;
        using var context = new AgendaDbContext(options);

        Action act = () => DbSeeder.SeedAdmin(
            context,
            _hasher,
            new SeedSettings { AdminEmail = "admin@agenda.com", AdminPassword = "Admin@123" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*agenda.db*");
    }
}
