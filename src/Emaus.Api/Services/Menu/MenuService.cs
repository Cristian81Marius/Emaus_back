using Emaus.Api.Dtos.Menu;

namespace Emaus.Api.Services.Menu;

/// <summary>Meniul principal (`GET /api/menu?project=`) — vezi docs/API.md §11 și §13. La scara
/// actuală, conținutul e hardcodat aici (nu editabil prin UI încă) — scopul rutei rămâne să
/// permită unui Nucleus să redenumească/reordoneze un tab din backend, fără update de aplicație.</summary>
public class MenuService
{
    public MenuConfigDto GetMenu(string? project)
    {
        return project?.Equals("bob", StringComparison.OrdinalIgnoreCase) == true ? BobMenu : EmausMenu;
    }

    private static readonly MenuConfigDto EmausMenu = new(
        Tabs:
        [
            new("locations", "Locații", "/", "🏠"),
            new("bookings", "Solicitări", "/bookings", "📋"),
            new("opportunities", "Activități", "/opportunities", "🤝"),
            new("menu", "Meniu", "/menu", "☰"),
        ],
        MoreSections:
        [
            new("Gestiune",
            [
                new("dashboard", "Dashboard", "/dashboard", "📊"),
                new("beneficiaries", "Beneficiari", "/beneficiaries", "🧑‍🤝‍🧑"),
                new("maintenance", "Mentenanță", "/maintenance", "🛠️"),
                new("cleaning", "Curățenie", "/cleaning", "🧹"),
                new("notifications", "Notificări", "/notifications", "🔔"),
                new("documents", "Documente utile", "/documents", "📄"),
            ]),
        ]);

    private static readonly MenuConfigDto BobMenu = new(
        Tabs:
        [
            new("bob-dashboard", "Dashboard", "/bob", "📦"),
            new("bob-beneficiaries", "Beneficiari", "/bob/beneficiaries", "🧑‍🤝‍🧑"),
            new("bob-shopping", "Cumpărături", "/bob/shopping", "🛒"),
            new("menu", "Meniu", "/menu", "☰"),
        ],
        MoreSections:
        [
            new("Box of Blessing",
            [
                new("bob-history", "Istoric cumpărături", "/bob/history", "🧾"),
            ]),
        ]);
}
