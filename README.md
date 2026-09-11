# Emaus API

Backend ASP.NET Core (.NET 8) pentru aplicația de organizare a cazărilor Emaus.

## Structură

```
backend/
  Emaus.sln
  src/
    Emaus.Domain/          entități + enumuri — nicio dependență externă
    Emaus.Infrastructure/   EF Core (AppDbContext, seed de date, hash de parole)
    Emaus.Api/              controllere, DTO-uri, autentificare JWT, Swagger
  schema.md                  diagrama bazei de date + raționamentul din spate
```

Trei proiecte, trei responsabilități — `Emaus.Api` depinde de `Emaus.Infrastructure`, care
depinde de `Emaus.Domain`, niciodată invers. Dacă adaugi ceva nou și nu știi unde intră:
o entitate/regulă de business pură → `Domain`; ceva ce ține de baza de date → `Infrastructure`;
un endpoint HTTP → `Api`.

## Rulare locală

Necesită [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd backend
dotnet restore
dotnet run --project src/Emaus.Api
```

La prima pornire, baza de date SQLite (`emaus.db`, creată lângă `Emaus.Api.csproj`) se
creează automat și se populează cu cele 8 locații din evidența actuală și un cont de test:

```
Telefon:  0700000000
Parolă:   Emaus2026!
```

Deschide `http://localhost:5017/swagger` — acolo poți testa toate endpoint-urile direct din
browser (buton „Authorize” sus, cu token-ul primit de la `/api/auth/login`). Fișierul
`Emaus.Api.http` are aceleași cereri gata scrise, dacă folosești VS Code cu extensia REST Client
sau Rider/Visual Studio (care înțeleg `.http` nativ).

## De ce SQLite pentru dezvoltare

Zero instalare — funcționează din prima cu `dotnet run`, fără server de bază de date separat.
`AppDbContext` nu ține nimic specific SQLite în el; când sunteți gata de producție, schimbați
un rând în `Program.cs` (`UseSqlite` → `UseNpgsql` sau `UseSqlServer`) și connection string-ul
din `appsettings.json`. Vezi `schema.md` → „Următorul pas tehnic” pentru migrațiile EF Core
reale (`dotnet ef migrations add InitialCreate`), necesare înainte de producție — dezvoltarea
locală funcționează fără ele (`Database.EnsureCreated()` generează schema direct din model).

## Configurare

| Cheie | Ce controlează | Unde |
|---|---|---|
| `ConnectionStrings:Default` | baza de date | `appsettings.json` |
| `Jwt:Secret` | semnătura token-urilor | `appsettings.Development.json` (dev) — în producție, `dotnet user-secrets` sau variabilă de mediu, **niciodată** în git |
| `Jwt:ExpiryMinutes` | cât rămâne valabilă o sesiune | `appsettings.json` |
| `Cors:AllowedOrigins` | ce adrese poate apela API-ul (aplicația mobilă/web) | `appsettings.Development.json` |

## Note despre acest scaffold

A fost scris integral, dar **nu a putut fi compilat în mediul în care a fost generat** —
sandbox-ul respectiv blochează accesul la `nuget.org`, deci pachetele EF Core/JWT/Swagger nu
s-au putut restaura acolo. Codul urmează exact tiparele standard ASP.NET Core 8 / EF Core 8
(niciun API obscur), dar rulează primul `dotnet restore && dotnet build` pe mașina ta ca primă
verificare — dacă apare vreo eroare de compilare, trimite-mi mesajul de eroare și o reparăm imediat.
