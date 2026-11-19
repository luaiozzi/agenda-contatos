using Agenda.Api.Data;
using Agenda.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Api.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AgendaDbContext _context;

    public ContactRepository(AgendaDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Contact>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Contact> query = _context.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            query = query.Where(c =>
                EF.Functions.Like(c.Name, pattern) ||
                EF.Functions.Like(c.Email, pattern) ||
                EF.Functions.Like(c.Phone, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Contact>(items, total, page, pageSize);
    }

    public Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Contacts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, int? ignoreId, CancellationToken cancellationToken)
    {
        IQueryable<Contact> query = _context.Contacts.Where(c => c.Email == email);

        if (ignoreId.HasValue)
        {
            var id = ignoreId.Value;
            query = query.Where(c => c.Id != id);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Contact contact, CancellationToken cancellationToken)
    {
        _context.Contacts.Update(contact);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Contact contact, CancellationToken cancellationToken)
    {
        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
