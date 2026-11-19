using Agenda.Api.Models;

namespace Agenda.Api.Repositories;

public interface IContactRepository
{
    Task<PagedResult<Contact>> GetPagedAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se já existe contato com o e-mail informado (já normalizado em minúsculas),
    /// ignorando opcionalmente o contato de Id <paramref name="ignoreId"/> (usado na edição).
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? ignoreId, CancellationToken cancellationToken);

    Task AddAsync(Contact contact, CancellationToken cancellationToken);

    Task UpdateAsync(Contact contact, CancellationToken cancellationToken);

    Task DeleteAsync(Contact contact, CancellationToken cancellationToken);
}
