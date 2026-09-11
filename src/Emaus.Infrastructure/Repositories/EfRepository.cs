using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Infrastructure.Repositories;

/// <summary>Implementare EF Core a <see cref="IRepository{TEntity}"/> — valabilă pentru orice
/// entitate, înregistrată o singură dată ca generic deschis în Program.cs
/// (`AddScoped(typeof(IRepository&lt;&gt;), typeof(EfRepository&lt;&gt;))`), nu per entitate.</summary>
public class EfRepository<TEntity>(AppDbContext db) : IRepository<TEntity> where TEntity : class
{
    public IQueryable<TEntity> Query() => db.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(Guid id) => await db.Set<TEntity>().FindAsync(id);

    public void Add(TEntity entity) => db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity) => db.Set<TEntity>().Remove(entity);
}
