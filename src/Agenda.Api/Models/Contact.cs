namespace Agenda.Api.Models;

/// <summary>
/// Entidade de domínio: um contato da agenda.
/// O telefone é armazenado apenas com dígitos (DDD + número) e o e-mail em minúsculas.
/// </summary>
public class Contact
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
