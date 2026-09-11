using Emaus.Domain.Entities;
using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;

namespace Emaus.Infrastructure.Repositories;

public class LocalityRepository(AppDbContext db) : ILocalityRepository
{
    public IQueryable<Locality> Query() => db.Localities;

    public async Task<Locality?> GetByIdAsync(int id) => await db.Localities.FindAsync(id);
}
