using System.Security.Claims;

namespace Emaus.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id-ul utilizatorului curent, extras din tokenul JWT validat de middleware-ul
    /// de autentificare. Aruncă dacă e apelat pe un endpoint fără [Authorize] — folosiți-l
    /// doar acolo unde autentificarea e garantată.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Cererea nu are un utilizator autentificat.");
        return Guid.Parse(raw);
    }
}
