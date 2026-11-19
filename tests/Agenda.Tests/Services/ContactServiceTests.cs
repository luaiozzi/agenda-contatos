using Agenda.Api.Dtos;
using Agenda.Api.Exceptions;
using Agenda.Api.Mappings;
using Agenda.Api.Models;
using Agenda.Api.Repositories;
using Agenda.Api.Services;
using Agenda.Api.Validators;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Agenda.Tests.Services;

public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _repository = new();
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _service = new ContactService(_repository.Object, new ContactRequestValidator());
    }

    private static ContactRequest ValidRequest() => new()
    {
        Name = "  Ana Souza ",
        Email = " Ana.Souza@Example.COM ",
        Phone = "(19) 99999-8888"
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ClampsPagingAndMapsItems()
    {
        var contacts = new List<Contact>
        {
            new() { Id = 1, Name = "Ana", Email = "ana@example.com", Phone = "19999998888" }
        };

        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<string?>(), 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Contact>(contacts, 1, 1, 100));

        var result = await _service.GetAllAsync("ana", 0, 1000, CancellationToken.None);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Email.Should().Be("ana@example.com");
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        _repository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Contact { Id = 3, Name = "Bruno", Email = "bruno@example.com", Phone = "1933334444" });

        var result = await _service.GetByIdAsync(3, CancellationToken.None);

        result.Id.Should().Be(3);
        result.Name.Should().Be("Bruno");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        Func<Task> act = () => _service.GetByIdAsync(99, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_WithValidRequest_NormalizesDataAndPersists()
    {
        Contact? saved = null;

        _repository
            .Setup(r => r.EmailExistsAsync("ana.souza@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) =>
            {
                contact.Id = 10;
                saved = contact;
            })
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(ValidRequest(), CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Ana Souza");
        saved.Email.Should().Be("ana.souza@example.com");
        saved.Phone.Should().Be("19999998888");
        saved.CreatedAt.Should().NotBe(default);

        result.Id.Should().Be(10);
        result.Email.Should().Be("ana.souza@example.com");
        result.Phone.Should().Be("19999998888");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictAndDoesNotPersist()
    {
        _repository
            .Setup(r => r.EmailExistsAsync("ana.souza@example.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => _service.CreateAsync(ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationAndDoesNotPersist()
    {
        var request = new ContactRequest { Name = "", Email = "invalido", Phone = "123" };

        Func<Task> act = () => _service.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesExistingContact()
    {
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new Contact
        {
            Id = 5,
            Name = "Antigo",
            Email = "antigo@example.com",
            Phone = "1933334444",
            CreatedAt = createdAt
        };

        _repository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repository
            .Setup(r => r.EmailExistsAsync("ana.souza@example.com", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateAsync(5, ValidRequest(), CancellationToken.None);

        existing.Id.Should().Be(5);
        existing.Name.Should().Be("Ana Souza");
        existing.Email.Should().Be("ana.souza@example.com");
        existing.Phone.Should().Be("19999998888");
        existing.CreatedAt.Should().Be(createdAt);
        existing.UpdatedAt.Should().NotBeNull();

        result.Id.Should().Be(5);
        result.Name.Should().Be("Ana Souza");

        _repository.Verify(
            r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        Func<Task> act = () => _service.UpdateAsync(404, ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(
            r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToAnotherContact_ThrowsConflict()
    {
        var existing = new Contact { Id = 5, Name = "Antigo", Email = "antigo@example.com", Phone = "1933334444" };

        _repository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repository
            .Setup(r => r.EmailExistsAsync("ana.souza@example.com", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => _service.UpdateAsync(5, ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _repository.Verify(
            r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsValidation()
    {
        var request = new ContactRequest { Name = "A", Email = "x", Phone = "1" };

        Func<Task> act = () => _service.UpdateAsync(5, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesContact()
    {
        var existing = new Contact { Id = 7, Name = "Carlos", Email = "carlos@example.com", Phone = "1933334444" };

        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _service.DeleteAsync(7, CancellationToken.None);

        _repository.Verify(
            r => r.DeleteAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        Func<Task> act = () => _service.DeleteAsync(8, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(
            r => r.DeleteAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
