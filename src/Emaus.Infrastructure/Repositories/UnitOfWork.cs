using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;

namespace Emaus.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}
