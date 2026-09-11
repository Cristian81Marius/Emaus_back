namespace Emaus.Domain.Repositories;

/// <summary>Commit explicit pentru schimbările făcute prin repository-uri. Un serviciu care
/// modifică mai multe entități (ex. `BookingService.CheckOut` schimbă și cazarea, și unitatea,
/// și creează o tură de curățenie) le salvează pe toate deodată, o singură dată, la final.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
