using Agenda.Api.Dtos;
using Agenda.Api.Models;

namespace Agenda.Api.Services;

public interface IContactService
{
    Task<PagedResult<ContactDto>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<ContactDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ContactDto> CreateAsync(ContactRequest request, CancellationToken cancellationToken);

    Task<ContactDto> UpdateAsync(int id, ContactRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
