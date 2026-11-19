using Agenda.Api.Models;

namespace Agenda.Api.Repositories;

public interface IUserRepository
{
    /// <summary>Busca um usuário pelo e-mail (já normalizado em minúsculas).</summary>
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}
