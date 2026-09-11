using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace Emaus.Api.Tests;

/// <summary>Testele nu trec prin Program.cs (unde se setează QuestPDF.Settings.License normal),
/// deci setăm licența o singură dată aici, la încărcarea assembly-ului de teste.</summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
