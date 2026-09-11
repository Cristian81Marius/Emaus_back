using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : EfRepository<ApplicationUser>(db), IUserRepository
{
    /// <summary>Întoarce contul indiferent de Status/IsActive — AuthService decide ce mesaj să
    /// dea (cont în așteptare, respins, dezactivat) DUPĂ ce verifică parola, ca să nu dezvăluie
    /// starea unui cont cuiva care doar a ghicit un telefon/email fără parola corectă.</summary>
    public async Task<ApplicationUser?> GetByIdentifierAsync(string identifier)
    {
        var trimmed = identifier.Trim();
        var normalizedEmail = trimmed.ToLowerInvariant();
        return await db.Users.SingleOrDefaultAsync(u =>
            u.Phone == trimmed || (u.Email != null && u.Email.ToLower() == normalizedEmail));
    }
}
