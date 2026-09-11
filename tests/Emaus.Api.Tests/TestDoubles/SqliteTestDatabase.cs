using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;
using Emaus.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Emaus.Api.Tests.TestDoubles;

/// <summary>Un `AppDbContext` REAL, peste o bază SQLite în memorie, unică per test — nu o
/// dublură simplificată. Motivul: EF Core face join-uri și "relationship fixup" reale prin
/// `Include()`, lucruri greu (și riscant) de simulat corect cu o listă în memorie și un
/// `IQueryable` fals — o listă falsă ar reproduce comportamentul EF Core doar aproximativ,
/// exact genul de discrepanță care ar ascunde bug-uri reale de query (vezi și bug-ul prins
/// în practică la implementare: SQLite nu poate traduce `Sum` pe `decimal`, ceva ce un fake
/// LINQ-to-Objects n-ar fi prins niciodată). O conexiune SQLite `:memory:` deschisă manual
/// (nu doar connection string) ține baza vie cât timp conexiunea nu e închisă — vezi Dispose.</summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    public IRepository<T> Repo<T>() where T : class => new EfRepository<T>(Db);
    public IUserRepository Users() => new UserRepository(Db);
    public ILocalityRepository Localities() => new LocalityRepository(Db);
    public IUnitOfWork UnitOfWork() => new UnitOfWork(Db);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
