using Agenda.Api.Models;

namespace Agenda.Api.Services;

/// <summary>Token gerado e o instante (UTC) em que expira.</summary>
public record TokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    TokenResult Generate(AppUser user);
}
