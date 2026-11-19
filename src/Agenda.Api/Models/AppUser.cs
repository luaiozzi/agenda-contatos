namespace Agenda.Api.Models;

/// <summary>
/// Usuário que pode autenticar na API.
/// A senha nunca é guardada em texto: somente o hash (PBKDF2, via ASP.NET Core Identity PasswordHasher).
/// </summary>
public class AppUser
{
    public int Id { get; set; }

    /// <summary>Sempre em minúsculas.</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
