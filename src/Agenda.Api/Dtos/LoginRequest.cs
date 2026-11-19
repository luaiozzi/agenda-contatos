namespace Agenda.Api.Dtos;

/// <summary>Credenciais enviadas no login.</summary>
public class LoginRequest
{
    /// <example>admin@agenda.com</example>
    public string? Email { get; set; }

    /// <example>Admin@123</example>
    public string? Password { get; set; }
}
