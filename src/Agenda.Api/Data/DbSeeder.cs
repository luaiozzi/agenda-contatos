using Agenda.Api.Models;
using Agenda.Api.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace Agenda.Api.Data;

public static class DbSeeder
{
    /// <summary>
    /// Cria o usuário administrador na primeira execução. Se ele já existir, não faz nada
    /// (a senha de um usuário existente nunca é sobrescrita).
    /// </summary>
    public static void SeedAdmin(AgendaDbContext db, IPasswordHasher<AppUser> hasher, SeedSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AdminEmail) || string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            return;
        }

        var email = settings.AdminEmail.Trim().ToLowerInvariant();

        try
        {
            if (db.Users.Any(u => u.Email == email))
            {
                return;
            }
        }
        catch (SqliteException ex) when (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "O banco de dados existente foi criado por uma versão anterior e não tem a tabela de usuários. " +
                "Apague o arquivo agenda.db (ou, no Docker, rode 'docker compose down -v') e inicie novamente.",
                ex);
        }

        var user = new AppUser
        {
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, settings.AdminPassword);

        db.Users.Add(user);
        db.SaveChanges();
    }
}
