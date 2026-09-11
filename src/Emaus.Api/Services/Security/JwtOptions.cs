namespace Emaus.Api.Services.Security;

/// <summary>Legat de secțiunea "Jwt" din appsettings.json. Secretul din appsettings.Development.json
/// e doar pentru dezvoltare locală — pentru producție, mutați-l într-un secret manager
/// (dotnet user-secrets, Azure Key Vault, variabilă de mediu), niciodată în cod sau în git.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpiryMinutes { get; set; } = 60 * 12; // 12 ore — o tură de voluntariat întreagă
}
