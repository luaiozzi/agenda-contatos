using Agenda.Api.Dtos;
using Agenda.Api.Models;

namespace Agenda.Api.Mappings;

/// <summary>
/// Conversões entre entidade e DTOs, escritas à mão.
/// Com apenas três campos, o mapeamento explícito é mais simples, mais rápido
/// e dispensa uma dependência extra.
/// </summary>
public static class ContactMappings
{
    public static ContactDto ToDto(this Contact contact)
    {
        return new ContactDto
        {
            Id = contact.Id,
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    public static Contact ToEntity(this ContactRequest request)
    {
        return new Contact
        {
            Name = request.Name ?? string.Empty,
            Email = request.Email ?? string.Empty,
            Phone = request.Phone ?? string.Empty
        };
    }

    /// <summary>Copia os campos editáveis para a entidade existente (Id e datas ficam intactos).</summary>
    public static void UpdateEntity(this ContactRequest request, Contact contact)
    {
        contact.Name = request.Name ?? string.Empty;
        contact.Email = request.Email ?? string.Empty;
        contact.Phone = request.Phone ?? string.Empty;
    }
}
