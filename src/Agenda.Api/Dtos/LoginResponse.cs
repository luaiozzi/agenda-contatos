namespace Agenda.Api.Dtos;

/// <summary>Resposta do login: token JWT e quando ele expira (UTC).</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string Email { get; set; } = string.Empty;
}
