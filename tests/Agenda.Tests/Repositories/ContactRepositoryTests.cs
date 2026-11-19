using Agenda.Api.Models;
using Agenda.Api.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Tests.Repositories;

public class ContactRepositoryTests : IDisposable
{
    private readonly SqliteTestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static Contact NewContact(string name, string email, string phone = "19999998888") => new()
    {
        Name = name,
        Email = email,
        Phone = phone,
        CreatedAt = DateTime.UtcNow
    };

    private async Task SeedAsync(params Contact[] contacts)
    {
        using var context = _db.CreateContext();
        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsContactAndGeneratesId()
    {
        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);
        var contact = NewContact("Ana", "ana@mail.com");

        await repository.AddAsync(contact, CancellationToken.None);

        contact.Id.Should().BeGreaterThan(0);

        using var verifyContext = _db.CreateContext();
        (await verifyContext.Contacts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsDbUpdateException()
    {
        await SeedAsync(NewContact("Ana", "ana@mail.com"));

        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);

        Func<Task> act = () => repository.AddAsync(NewContact("Outra Ana", "ana@mail.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByNameAndPaginates()
    {
        await SeedAsync(
            NewContact("Carlos", "carlos@mail.com"),
            NewContact("Ana", "ana@mail.com"),
            NewContact("Bruno", "bruno@mail.com"));

        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);

        var firstPage = await repository.GetPagedAsync(null, 1, 2, CancellationToken.None);
        var secondPage = await repository.GetPagedAsync(null, 2, 2, CancellationToken.None);

        firstPage.Total.Should().Be(3);
        firstPage.Items.Select(c => c.Name).Should().Equal("Ana", "Bruno");
        secondPage.Items.Select(c => c.Name).Should().Equal("Carlos");
    }

    [Fact]
    public async Task GetPagedAsync_WithSearch_FiltersByNameEmailOrPhone()
    {
        await SeedAsync(
            NewContact("Ana Souza", "ana@mail.com", "19911112222"),
            NewContact("Bruno Lima", "bruno@mail.com", "11933334444"),
            NewContact("Carlos Dias", "carlos@mail.com", "21955556666"));

        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);

        var byName = await repository.GetPagedAsync("souza", 1, 10, CancellationToken.None);
        var byEmail = await repository.GetPagedAsync("bruno@", 1, 10, CancellationToken.None);
        var byPhone = await repository.GetPagedAsync("5555", 1, 10, CancellationToken.None);
        var noMatch = await repository.GetPagedAsync("zzzz", 1, 10, CancellationToken.None);

        byName.Items.Should().ContainSingle().Which.Name.Should().Be("Ana Souza");
        byEmail.Items.Should().ContainSingle().Which.Name.Should().Be("Bruno Lima");
        byPhone.Items.Should().ContainSingle().Which.Name.Should().Be("Carlos Dias");
        noMatch.Total.Should().Be(0);
        noMatch.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsContactOrNull()
    {
        var contact = NewContact("Ana", "ana@mail.com");
        await SeedAsync(contact);

        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);

        var found = await repository.GetByIdAsync(contact.Id, CancellationToken.None);
        var missing = await repository.GetByIdAsync(9999, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Email.Should().Be("ana@mail.com");
        missing.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_RespectsIgnoreId()
    {
        var contact = NewContact("Ana", "ana@mail.com");
        await SeedAsync(contact);

        using var context = _db.CreateContext();
        var repository = new ContactRepository(context);

        (await repository.EmailExistsAsync("ana@mail.com", null, CancellationToken.None)).Should().BeTrue();
        (await repository.EmailExistsAsync("ana@mail.com", contact.Id, CancellationToken.None)).Should().BeFalse();
        (await repository.EmailExistsAsync("ana@mail.com", contact.Id + 1, CancellationToken.None)).Should().BeTrue();
        (await repository.EmailExistsAsync("outro@mail.com", null, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var contact = NewContact("Ana", "ana@mail.com");
        await SeedAsync(contact);

        using (var context = _db.CreateContext())
        {
            var repository = new ContactRepository(context);
            var tracked = await repository.GetByIdAsync(contact.Id, CancellationToken.None);
            tracked!.Name = "Ana Maria";
            tracked.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(tracked, CancellationToken.None);
        }

        using var verifyContext = _db.CreateContext();
        var saved = await verifyContext.Contacts.SingleAsync();
        saved.Name.Should().Be("Ana Maria");
        saved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesContact()
    {
        var contact = NewContact("Ana", "ana@mail.com");
        await SeedAsync(contact);

        using (var context = _db.CreateContext())
        {
            var repository = new ContactRepository(context);
            var tracked = await repository.GetByIdAsync(contact.Id, CancellationToken.None);

            await repository.DeleteAsync(tracked!, CancellationToken.None);
        }

        using var verifyContext = _db.CreateContext();
        (await verifyContext.Contacts.CountAsync()).Should().Be(0);
    }
}
