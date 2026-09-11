using Emaus.Domain.Entities;

namespace Emaus.Domain.Repositories;

/// <summary>A doua excepție de la repository-ul generic (vezi <see cref="IRepository{TEntity}"/>).
/// `Locality` are cheie `int`, nu `Guid` ca restul entităților, și e un tabel de referință
/// populat o singură dată la seed — doar citit, niciodată scris prin API — de-aia interfața
/// n-are Add/Remove.</summary>
public interface ILocalityRepository
{
    IQueryable<Locality> Query();
    Task<Locality?> GetByIdAsync(int id);
}
