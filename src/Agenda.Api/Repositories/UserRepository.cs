using Agenda.Api.Data;
using Agenda.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AgendaDbContext _context;

    public UserRepository(AgendaDbContext context)
    {
        _context = context;
    }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}
