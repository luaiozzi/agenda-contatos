namespace Agenda.Api.Dtos;

/// <summary>
/// Representação de um contato retornada pela API.
/// </summary>
public class ContactDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Somente dígitos, ex.: 19999998888.</summary>
    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
