using Emaus.Domain.Entities;

namespace Emaus.Domain.Repositories;

/// <summary>Singura interfață de repository cu o interogare cu adevărat proprie: login-ul
/// caută un utilizator după telefon SAU email, înainte de a avea id-ul lui.</summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    /// <summary>Identifier = telefon SAU email (comparație case-insensitive pe email).</summary>
    Task<ApplicationUser?> GetByIdentifierAsync(string identifier);
}
