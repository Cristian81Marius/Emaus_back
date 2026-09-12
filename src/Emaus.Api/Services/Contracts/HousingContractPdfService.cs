using Emaus.Api.Common;
using Emaus.Api.Dtos.Documents;
using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

namespace Emaus.Api.Services.Contracts;

/// <summary>Completează template-ul AcroForm real al contractului de cazare (creat în Sejda,
/// vezi Assets/Documents/housing-contract-template.pdf) în loc să reconstruiască textul legal
/// din cod (asta face BookingContractDocument/QuestPDF, pentru contractul generat din
/// Bookings — rămâne neschimbat; acesta e fluxul folosit de ecranul mobil
/// /documents/housing-contract, unde datele vin direct din formular, nu dintr-o cazare
/// existentă). Numele câmpurilor de mai jos sunt EXACT cele din PDF — verificate o dată cu
/// pdf-lib (vezi lista din conversație), nu le schimbați fără să recitiți PDF-ul.</summary>
public sealed class HousingContractPdfService
{
    private static readonly string TemplatePath =
        System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Documents", "housing-contract-template.pdf");

    /// <summary>Singurul câmp care NU e text — un câmp de tip semnătură digitală (AcroForm
    /// "Signature"), nefolosit aici pentru semnare criptografică reală. Nu poate primi o
    /// imagine prin SetValue ca un câmp text; e eliminat din formular și înlocuit cu desenul
    /// direct al PNG-ului, la exact poziția/pagina lui (vezi InsertSignature).</summary>
    private const string SignatureFieldName = "signature_3tuvk";

    /// <summary>Fișierul PDF gol (fără AcroForm completat) — pentru cineva care vrea să-l
    /// printe și să-l completeze de mână, sau doar să vadă cum arată contractul înainte de
    /// a-l completa din aplicație. Vezi DocumentsController → GET .../housing-contract/blank.</summary>
    public ServiceResult<byte[]> GetBlankTemplate()
    {
        if (!File.Exists(TemplatePath))
            return ServiceResult<byte[]>.NotFound("Template-ul contractului de cazare lipsește de pe server.");

        return ServiceResult<byte[]>.Ok(File.ReadAllBytes(TemplatePath));
    }

    public ServiceResult<byte[]> Fill(HousingContractFillRequest request)
    {
        if (!File.Exists(TemplatePath))
            return ServiceResult<byte[]>.NotFound("Template-ul contractului de cazare lipsește de pe server.");

        // Opțională — cineva poate exporta contractul completat fără semnătură (ex. de
        // printat și semnat pe hârtie ulterior). `null` înseamnă "fără semnătură", nu o
        // eroare; doar un string nevid dar invalid ca base64 e o eroare de request.
        byte[]? signatureBytes = null;
        if (!string.IsNullOrWhiteSpace(request.SignaturePngBase64))
        {
            try
            {
                signatureBytes = DecodeSignature(request.SignaturePngBase64);
            }
            catch (FormatException)
            {
                return ServiceResult<byte[]>.Invalid("Semnătura nu e un PNG valid codat base64.");
            }
        }

        using var ms = new MemoryStream();
        using (var reader = new PdfReader(TemplatePath))
        using (var writer = new PdfWriter(ms))
        using (var pdfDoc = new PdfDocument(reader, writer))
        {
            var form = PdfAcroForm.GetAcroForm(pdfDoc, true);

            SetIfPresent(form, "nume_contact_urgenta", request.NumeContactUrgenta);
            SetIfPresent(form, "telefon_contact_urgenta", request.TelefonContactUrgenta);
            SetIfPresent(form, "telefon_benef", request.TelefonBenef);
            SetIfPresent(form, "numar", request.Numar);
            SetIfPresent(form, "seria", request.Seria);
            SetIfPresent(form, "adresa", request.Adresa);
            SetIfPresent(form, "judet", request.Judet);
            SetIfPresent(form, "localitate", request.Localitate);
            SetIfPresent(form, "nume_complet_benef", request.NumeCompletBenef);
            SetIfPresent(form, "nume_responsabil_cazare", request.NumeResponsabilCazare);
            SetIfPresent(form, "ziua_inceput", request.ZiuaInceput);
            SetIfPresent(form, "luna_inceput", request.LunaInceput);
            SetIfPresent(form, "an_inceput", request.AnInceput);
            SetIfPresent(form, "an_sfarsit", request.AnSfarsit);
            SetIfPresent(form, "luna_sfarsit", request.LunaSfarsit);
            SetIfPresent(form, "zi_sfarsit", request.ZiSfarsit);
            SetIfPresent(form, "complet_name_benef", request.CompletNameBenef);
            SetIfPresent(form, "locatia_cazarii", request.LocatiaCazarii);
            SetIfPresent(form, "data_cazarii", request.DataCazarii);

            InsertSignature(pdfDoc, form, signatureBytes);

            form.FlattenFields();
        }

        return ServiceResult<byte[]>.Ok(ms.ToArray());
    }

    private static void SetIfPresent(PdfAcroForm form, string fieldName, string? value)
    {
        var field = form.GetField(fieldName);
        field?.SetValue(value ?? string.Empty);
    }

    private static byte[] DecodeSignature(string base64Png)
    {
        var commaIndex = base64Png.IndexOf(',');
        var raw = commaIndex >= 0 ? base64Png[(commaIndex + 1)..] : base64Png;
        return Convert.FromBase64String(raw);
    }

    /// <summary><paramref name="signaturePng"/> null = fără semnătură (câmpul rămâne gol,
    /// doar scos din formular ca să nu apară interactiv/needitabil în PDF-ul final). Altfel,
    /// desenat la poziția exactă a widget-ului, apoi câmpul original tot e scos — un câmp de
    /// tip semnătură (/Sig) nu poate ține o imagine ca un câmp text obișnuit.</summary>
    private static void InsertSignature(PdfDocument pdfDoc, PdfAcroForm form, byte[]? signaturePng)
    {
        var field = form.GetField(SignatureFieldName);
        if (field is null) return;

        if (signaturePng is not null)
        {
            var widgets = field.GetWidgets();
            if (widgets.Count > 0)
            {
                var widget = widgets[0];
                Rectangle rect = widget.GetRectangle().ToRectangle();
                PdfPage page = widget.GetPage();
                var imageData = ImageDataFactory.Create(signaturePng);
                var canvas = new PdfCanvas(page);
                canvas.AddImageFittedIntoRectangle(imageData, rect, false);
            }
        }

        form.RemoveField(SignatureFieldName);
    }
}
