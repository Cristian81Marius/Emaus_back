namespace Emaus.Domain.Repositories;

/// <summary>Repository generic minimal — un singur punct de interogare (<see cref="Query"/>)
/// peste care serviciile compun `.Include()`/`.Where()` după nevoie, plus Add/Remove pentru
/// scriere. Nu există `SaveAsync()` aici: commit-ul se face explicit prin
/// <see cref="IUnitOfWork"/>, ca un serviciu să poată modifica mai multe entități
/// (ex. o cazare + unitatea ei) și să le salveze într-o singură tranzacție.
///
/// De ce un singur repository generic în loc de o interfață per entitate (IPropertyRepository,
/// IBookingRepository, ...)? Fiindcă niciuna dintre entitățile aplicației nu are azi nevoie de
/// interogări atât de specifice încât să merite propria interfață — `Query()` + LINQ acoperă
/// tot. Excepțiile: <see cref="IUserRepository"/> (căutare după telefon, pentru login) și
/// <see cref="ILocalityRepository"/> (cheie `int`, nu `Guid`, deci nu se potrivește pe acest
/// generic). Dacă o entitate capătă reguli de interogare cu adevărat proprii, îi dai o
/// interfață dedicată atunci, nu dinainte.</summary>
public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(Guid id);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}
