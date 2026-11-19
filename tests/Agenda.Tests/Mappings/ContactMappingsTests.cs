using Agenda.Api.Dtos;
using Agenda.Api.Mappings;
using Agenda.Api.Models;
using FluentAssertions;

namespace Agenda.Tests.Mappings;

public class ContactMappingsTests
{
    [Fact]
    public void ToDto_CopiesAllFields()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var contact = new Contact
        {
            Id = 4,
            Name = "Ana",
            Email = "ana@example.com",
            Phone = "19999998888",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        var dto = contact.ToDto();

        dto.Id.Should().Be(4);
        dto.Name.Should().Be("Ana");
        dto.Email.Should().Be("ana@example.com");
        dto.Phone.Should().Be("19999998888");
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void ToEntity_CopiesEditableFieldsOnly()
    {
        var request = new ContactRequest { Name = "Ana", Email = "ana@example.com", Phone = "19999998888" };

        var contact = request.ToEntity();

        contact.Id.Should().Be(0);
        contact.Name.Should().Be("Ana");
        contact.Email.Should().Be("ana@example.com");
        contact.Phone.Should().Be("19999998888");
        contact.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateEntity_KeepsIdAndCreatedAt()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var contact = new Contact { Id = 9, Name = "Antigo", Email = "a@a.com", Phone = "1933334444", CreatedAt = createdAt };
        var request = new ContactRequest { Name = "Novo", Email = "novo@example.com", Phone = "19999998888" };

        request.UpdateEntity(contact);

        contact.Id.Should().Be(9);
        contact.CreatedAt.Should().Be(createdAt);
        contact.Name.Should().Be("Novo");
        contact.Email.Should().Be("novo@example.com");
        contact.Phone.Should().Be("19999998888");
    }
}
