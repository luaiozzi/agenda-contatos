using Agenda.Api.Dtos;
using Agenda.Api.Exceptions;
using Agenda.Api.Helpers;
using Agenda.Api.Mappings;
using Agenda.Api.Models;
using Agenda.Api.Repositories;
using FluentValidation;

namespace Agenda.Api.Services;

/// <summary>
/// Regras de negócio da agenda:
/// 1. Nome, e-mail e telefone são obrigatórios e validados (FluentValidation).
/// 2. O e-mail é normalizado (trim + minúsculas) e deve ser único.
/// 3. O telefone é normalizado para somente dígitos (DDD + número).
/// 4. Listagem paginada (máx. 100 por página), ordenada por nome, com busca por nome/e-mail/telefone.
/// </summary>
public class ContactService : IContactService
{
    private const int MaxPageSize = 100;

    private readonly IContactRepository _repository;
    private readonly IValidator<ContactRequest> _validator;

    public ContactService(IContactRepository repository, IValidator<ContactRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<PagedResult<ContactDto>> GetAllAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await _repository.GetPagedAsync(search, page, pageSize, cancellationToken);
        var items = result.Items.Select(c => c.ToDto()).ToList();

        return new PagedResult<ContactDto>(items, result.Total, page, pageSize);
    }

    public async Task<ContactDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var contact = await FindOrThrowAsync(id, cancellationToken);
        return contact.ToDto();
    }

    public async Task<ContactDto> CreateAsync(ContactRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var normalized = Normalize(request);

        if (await _repository.EmailExistsAsync(normalized.Email!, null, cancellationToken))
        {
            throw new ConflictException("Já existe um contato cadastrado com este e-mail.");
        }

        var contact = normalized.ToEntity();
        contact.CreatedAt = DateTime.UtcNow;

        await _repository.AddAsync(contact, cancellationToken);

        return contact.ToDto();
    }

    public async Task<ContactDto> UpdateAsync(int id, ContactRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var contact = await FindOrThrowAsync(id, cancellationToken);
        var normalized = Normalize(request);

        if (await _repository.EmailExistsAsync(normalized.Email!, id, cancellationToken))
        {
            throw new ConflictException("Já existe outro contato cadastrado com este e-mail.");
        }

        normalized.UpdateEntity(contact);
        contact.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(contact, cancellationToken);

        return contact.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var contact = await FindOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(contact, cancellationToken);
    }

    private async Task<Contact> FindOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var contact = await _repository.GetByIdAsync(id, cancellationToken);

        if (contact is null)
        {
            throw new NotFoundException($"Contato com id {id} não foi encontrado.");
        }

        return contact;
    }

    private static ContactRequest Normalize(ContactRequest request)
    {
        return new ContactRequest
        {
            Name = (request.Name ?? string.Empty).Trim(),
            Email = (request.Email ?? string.Empty).Trim().ToLowerInvariant(),
            Phone = PhoneNormalizer.Normalize(request.Phone)
        };
    }
}
