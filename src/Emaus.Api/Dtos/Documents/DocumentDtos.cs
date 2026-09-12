namespace Emaus.Api.Dtos.Documents;

/// <summary>Câmpurile textului AcroForm din "Contract beneficiari - complet (auto_complet).pdf"
/// (template creat în Sejda, vezi Assets/Documents/housing-contract-template.pdf) — numele
/// proprietăților reflectă exact numele câmpurilor din PDF (vezi comentariul de pe
/// HousingContractPdfService), ca mapping-ul să rămână evident fără a mai recitit PDF-ul.
/// Toate valorile vin gata formatate de pe frontend (ex. ziua/luna/anul ca stringuri separate,
/// exact cum apar pe formular) — serviciul doar le scrie în câmpuri, nu le validează/formatează.</summary>
public sealed record HousingContractFillRequest
{
    // Pagina 1 — identitatea beneficiarului și persoana de contact
    public string? NumeCompletBenef { get; init; }
    public string? TelefonBenef { get; init; }
    public string? Seria { get; init; }
    public string? Numar { get; init; }
    public string? Adresa { get; init; }
    public string? Localitate { get; init; }
    public string? Judet { get; init; }
    public string? NumeContactUrgenta { get; init; }
    public string? TelefonContactUrgenta { get; init; }
    public string? NumeResponsabilCazare { get; init; }

    // Pagina 2 — perioada de cazare
    public string? ZiuaInceput { get; init; }
    public string? LunaInceput { get; init; }
    public string? AnInceput { get; init; }
    public string? ZiSfarsit { get; init; }
    public string? LunaSfarsit { get; init; }
    public string? AnSfarsit { get; init; }

    // Pagina 5 — rândul de semnătură
    public string? CompletNameBenef { get; init; }
    public string? LocatiaCazarii { get; init; }
    public string? DataCazarii { get; init; }

    /// <summary>PNG codat base64 (cu sau fără prefixul "data:image/png;base64,") — o singură
    /// semnătură, a beneficiarului; desenată peste câmpul de semnătură al PDF-ului (vezi
    /// HousingContractPdfService.InsertSignature). OPȚIONAL — cineva poate genera/descărca
    /// contractul completat și fără semnătură (ex. de printat și semnat pe hârtie), câmpul
    /// de semnătură rămâne atunci gol.</summary>
    public string? SignaturePngBase64 { get; init; }
}
