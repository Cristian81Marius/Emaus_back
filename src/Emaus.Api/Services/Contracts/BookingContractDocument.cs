using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Emaus.Api.Services.Contracts;

/// <summary>Datele variabile ale contractului — restul (identitatea A.O.D.B., clauzele
/// V-VIII) e text fix, reprodus din contractul real folosit de asociație
/// ("Contract beneficiari - complet.pdf"). Câmpurile lipsă (beneficiarul nu are încă
/// C.I./adresă/persoană de sprijin completate) apar ca linie punctată de completat manual —
/// exact ca pe formularul pe hârtie — nu blochează generarea.</summary>
public sealed record BookingContractData(
    string BeneficiaryFullName,
    string? BeneficiaryLocalityName,
    string? BeneficiaryCounty,
    string? BeneficiaryAddress,
    string? BeneficiaryIdCardSeries,
    string? BeneficiaryIdCardNumber,
    string? BeneficiaryPhone,
    string? SupportPersonName,
    string? SupportPersonPhone,
    string? CaseManagerName,
    string? CaseManagerPhone,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut);

/// <summary>Generează PDF-ul "Contract de acordare a serviciilor sociale" — vezi
/// BookingService.GenerateContractPdfAsync. Text integral reprodus din contractul real al
/// asociației; NU parafrazat — orice modificare de conținut legal trebuie făcută cu grijă,
/// în acord cu varianta pe hârtie folosită de Centrul EMAUS.</summary>
public sealed class BookingContractDocument(BookingContractData data) : IDocument
{
    private const string AssociationName = "Asociația „Oastea Domnului București”";
    private const string Blank = "..................................";

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9.5f).LineHeight(1.15f));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(t =>
            {
                t.CurrentPageNumber();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Mișcarea „Oastea Domnului”").Bold().FontSize(9);
                col.Item().Text("– în cadrul Bisericii Ortodoxe Române").FontSize(8);
                col.Item().Text(AssociationName).Bold().FontSize(9);
                col.Item().Text("C.U.I. 47736462, str. Principală nr. 170").FontSize(8);
                col.Item().Text("Cont (lei): RO46BTRLRONCRT0669490501").FontSize(8);
                col.Item().Text("deschis la Banca Transilvania, filiala București").FontSize(8);
            });
            row.ConstantItem(140).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("Proiect").FontSize(9);
                col.Item().AlignRight().Text("„Centrul EMAUS”").Bold().FontSize(10);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().AlignCenter().Text("CONTRACT DE ACORDARE A SERVICIILOR SOCIALE").Bold().FontSize(12);
            column.Item().AlignCenter().Text($"Nr. {Blank} / data {Blank}");

            Heading(column, "I. PĂRŢILE CONTRACTULUI");
            column.Item().Text("Contractul se încheie între:").Bold();

            column.Item().Text(t =>
            {
                t.Justify();
                t.Span($"{AssociationName} (denumită în continuare și A.O.D.B.), cu sediul social în jud. Dâmbovița, comuna Vulcana-Pandele, str. Principală nr. 170, CUI: 47736462, tel. +40753 551 526 având contul bancar (lei): RO46BTRLRONCRT0669490501 deschis la Banca Transilvania, și punct de lucru în București, str. Drumul Taberei 82, reprezentată prin dl. Sandu Samuel, identificat cu C.I. Seria ZD, Nr. 073611, în calitate de președinte al asociației și manager al Proiectului „Centrul EMAUS”. Manager de caz, din partea A.O.D.B., a fost desemnat(ă): ");
                t.Span(OrBlank(data.CaseManagerName)).Bold();
                t.Span(", telefon: ");
                t.Span(OrBlank(data.CaseManagerPhone)).Bold();
                t.Span(";");
            });

            column.Item().Text("și");

            column.Item().Text(t =>
            {
                t.Justify();
                t.Span("D-l/D-na ");
                t.Span(data.BeneficiaryFullName).Bold();
                t.Span(", cu domiciliul stabil în loc. ");
                t.Span(OrBlank(data.BeneficiaryLocalityName)).Bold();
                t.Span(", jud. ");
                t.Span(OrBlank(data.BeneficiaryCounty)).Bold();
                t.Span(", Adresa: ");
                t.Span(OrBlank(data.BeneficiaryAddress)).Bold();
                t.Span(", posesor al C.I. Seria ");
                t.Span(OrBlank(data.BeneficiaryIdCardSeries)).Bold();
                t.Span(" Nr. ");
                t.Span(OrBlank(data.BeneficiaryIdCardNumber)).Bold();
                t.Span(", telefon: ");
                t.Span(OrBlank(data.BeneficiaryPhone)).Bold();
                t.Span(", în calitate de beneficiar al activităților de asistență socială, desfășurate de către " +
                       AssociationName + " în cadrul Proiectului „Centrul EMAUS”. Persoana de sprijin, desemnată " +
                       "de beneficiar pentru situația în care își pierde autonomia, pe perioada acordării " +
                       "serviciilor prevăzute prin prezentul contract, este: ");
                t.Span(OrBlank(data.SupportPersonName)).Bold();
                t.Span(", telefon: ");
                t.Span(OrBlank(data.SupportPersonPhone)).Bold();
                t.Span(".");
            });

            Heading(column, "II. OBIECTUL CONTRACTULUI");
            Paragraph(column,
                "Obiectul contractului îl constituie stabilirea şi reglementarea relaţiei de asistare, dintre " +
                "A.O.D.B. şi persoana care beneficiaza de serviciile sociale oferite în cadrul Proiectului " +
                "„Centrul EMAUS”;");

            Heading(column, "III. SCOPUL CONTRACTULUI");
            Paragraph(column,
                "Scopul relației de asistare vizează sprijinirea persoanei beneficiare în vederea gestionării " +
                "sau soluționării dificultăților sociale cu care se confuntă, constatate prin ancheta socială;");

            Heading(column, "IV. DURATA CONTRACTULUI");
            column.Item().Text(t =>
            {
                t.Justify();
                t.Span("Prezentul contract este valabil de la data de ");
                t.Span(data.RequestedCheckIn.ToString("dd.MM.yyyy")).Bold();
                t.Span(" până la data de ");
                t.Span(data.RequestedCheckOut.ToString("dd.MM.yyyy")).Bold();
                t.Span(".");
            });

            Heading(column, "V. DREPTURILE ŞI OBLIGAŢIILE PĂRŢILOR");
            SubHeading(column, "A. Drepturile Asociația „Oastea Domnului București” (A.O.D.B.):");
            LetteredList(column,
                "A.O.D.B. își rezervă dreptul să accepte şi să inscrie în Proiectul „Centrul EMAUS” doar persoanele care îndeplinesc criteriile de eligibilitate și care se angajează să respecte prevederile prezentului contract;",
                "A.O.D.B. are dreptul de a avea acces la informaţiile şi documentele necesare întocmirii dosarului beneficiarului, cu respectarea confidenţialităţii acestora;",
                "A.O.D.B. are dreptul să solicite accesul, pe tot parcursul perioadei de asistare, la informații corecte și complete, privind situaţia beneficiarului, în vederea stabilirii unor măsuri adecvate de sprijin;",
                "A.O.D.B. își rezervă dreptul de a face sesizări și de a furniza informații către instituțiile competente ale statului, vis-a-vis de orice situație în care este angajat beneficiarul și care contravine legii, moralei ori siguranței/bunăstării altor persoane respectiv vis-a-vis de orice abuz la adresa voluntarilor A.O.D.B. și a terților implicați/afectați;",
                "A.O.D.B. își rezervă dreptul de a înceta sprijinul oferit beneficiarului, în urma unui anunț mesaj transmis telefonic în prealabil, fără niciun fel de repercursiuni, ca urmare a intrării asociației, din orice motive, în imposibilitatea de a-și desfășura activitatea respectiv a unor carențe grave de comunicare/colaborare cu beneficiarul, în procesul de asistare;",
                "A.O.D.B. își rezervă dreptul de a reevalua/modifica forma și valoarea sprijinului oferit beneficiarului, cu anunțarea sa prealabilă (prin mesaj transmis telefonic), ca urmare a unor schimbări intervenite la nivelul fondurilor și resurselor disponibile pentru implementarea proiectelor curente;",
                "A.O.D.B. are dreptul de a-l informa pe beneficiar cu privire la activitățile pe care asociația le desfășoară, inclusiv cele de natură moral-religioasă, respectiv de a-i oferi materiale informative / educative și de a-l invita la diverse activități desfășurate – acesta având dreptul, la rândul său, de a primi / respinge ofertele și propunerile asociației;",
                "A.O.D.B. are dreptul de a refuza să divulge oricui orice fel de informații în măsura în care apreciază că persoana care le solicită este rău intenționată la adresa organizației, a voluntarilor / personalului ori a beneficiarilor săi – excepție făcând reprezentanții autorităților Statului Român;",
                "A.O.D.B. își rezervă dreptul de a verifica oricând locuința oferită beneficiarului – în prezența acestuia, în intervalul orar 8.00-20.00, dacă are suspiciuni că unele prevederi ale acestui contract sunt încălcate, și oricând în intervalul orar 20.00-8.00, la sesizarea co-locatarilor ori a reprezentanților Asociației de Proprietari / Locatari – cu asistența organelor abilitate de lege, dacă apreciază că este cazul;");

            SubHeading(column, "B. Obligații ale Asociația „Oastea Domnului București” (A.O.D.B.):");
            LetteredList(column,
                "A.O.D.B. are obligația de a acorda servicii sociale cu respect pentru beneficiar și fără discriminare, în funcţie de rasă, etnie, sex, convingeri politice sau religioase şi alte criterii asemănătoare;",
                "A.O.D.B. își asumă obligația de a oferi beneficiarului sprijin prin cazare gratuită, într-una dintre locațiile Centrului EMAUS, menționată anterior ca punct de lucru, conform prevederilor prezentului contract (mai ales, pe perioada de valabilitate) și în limitele resurselor disponibile;",
                "A.O.D.B. își asumă obligația de a achita cheltuielile privind utilitățile locuinței oferite gratuit beneficiarului;",
                "A.O.D.B. își asumă obligația de a asigura igienizarea locuinței o singură dată, anterior predării acesteia către beneficiar, asigurându-o 24 h nefolosită între perioadele de cazare a doi beneficiari diferiți;",
                "Reprezentanții A.O.D.B. au obligația de a anunța, cu minimum 1 h înainte, intenția de a vizita locuința oferită beneficiarului și de a nu o vizita în absența acestuia;",
                "A.O.D.B. își asumă obligația de a oferi gratuit beneficiarului sprijin emoțional, moral, spiritual, precum și sub orice altă formă posibilă, în limitele competențelor voluntarilor în proiect și atât cât beneficiarul va consimți;",
                "Dacă va fi nevoie și dacă va fi posibil, voluntarii asociației se angajează să facă lobby în interesul beneficiarului pe lângă instituții publice sau persoane (fizice/juridice) private, în vederea soluționării problemelor medico-sociale cu care se confruntă – însă fără nici un angajament de soluționare pozitivă;",
                "A.O.D.B. își propune să organizeze activități gratuite de socializare pentru beneficiari, în măsura în care va fi posibil și beneficiarii vor dori;",
                "A.O.D.B. nu are obligația de a desfășura activități / acțiuni de asistență socială sau de sprijin care depășesc resursele (material-financiare, umane, locative etc.) de care dispune respectiv ori care nu sunt circumscrise valorilor / misiunii / obiectivelor sale;",
                "A.O.D.B. se obligă să păstreze confidenţialitatea informaţiilor obţinute cu privire la situaţia psiho-socio-materială a beneficiarului şi a familiei acestuia respectiv de a îi respecta individualitatea și dreptul la autodeterminare;",
                "A.O.D.B. nu are obligația de a ajuta nicio persoană care nu respectă criteriile de eligibilitate ale Centrului EMAUS, regulamentul de ordine interioară și orice alte documente interne de organizare și funcționare;",
                "A.O.D.B. nu are obligația de a continua relația de sprijin cu beneficiari care nu-i respectă valorile / principiile de acțiune / personalul;");

            column.Item().PageBreak();

            SubHeading(column, "C. Alte drepturi și obligații ale beneficiarului:");
            LetteredList(column,
                "Dreptul de a obţine informaţii corecte si complete cu privire la serviciile acordate de către " + AssociationName + ";",
                "Dreptul de a apela la oricare dintre serviciile oferite de A.O.D.B.;",
                "Obligația de a obține sau de a completa documente justificative, solicitate de reprezentanții asociației, în vederea asistării (acte personale, contract de asistare, plan de intervenție, procese verbale de donație ș.a.m.d.);",
                "Obligația de a colabora deschis cu reprezentanții A.O.D.B., pe parcursul procesului de asistare;",
                "Dreptul de a refuza divulgarea oricăror informații privitoare la propria persoană sau situație;",
                "Dreptul de a beneficia de cazare gratuită în cadrul locațiilor Centrului EMAUS, pe perioada de valabilitate și cu respectarea clauzelor prezentului contract;",
                "Beneficiarul are obligația de a-și soluționa orice conflict / neînțelegere cu persoane din imobilul de locuințe ori cu Asociația de Proprietari / Locatari fără să implice A.O.D.B.;",
                "Beneficiarul este direct și integral răspunzător pentru buna păstrare a oricăror obiecte de inventar din locuință;",
                "În cazul în care a produs pagube locuinței în care a fost găzduit, beneficiarul se angajează să achite contravaloarea produselor / aparaturii / etc.;",
                "Beneficiarul se angajează să predea cheia locuinței, din cadrul Centrului EMAUS, reprezentantului A.O.D.B. – la finalizarea intervenției – fără a realiza o copie a acesteia, înțelegând că orice tentativă de acces ulterior în aceiași locuință – cu copia realizată a cheii și fără acordul A.O.D.B. – constituie infracțiune sancționată de Codul Penal;",
                "La plecare, în măsura în care starea de sănătate îi permite, beneficiarul este încurajat să lase locuința în stare de curățenie;",
                "Beneficiarul nu poate să-și prelungească sub niciun motiv șederea în locuința Centrului EMAUS după încheierea perioadei de valabilitate a prezentului contract, mai ales în condițiile în care reprezentanții A.O.D.B. nu i-o acordă și există o altă persoană programată pentru a beneficia de sprijin în perioada următoare;",
                "Beneficiarul înțelege și este de acord că orice tentativă de prelungire a șederii în locuința Centrului EMAUS, în afara perioadei stabilite prin prezentul contract sau ulterior nerespectării oricăreia dintre prevederile contractuale, constituie infracțiune și va fi sesizată Organelor de Poliție din București – urmând ca beneficiarul să-și asume consecințele;",
                "Dreptul de a nu achita nicio cheltuială solicitată ori propusă de A.O.D.B. cu privire la care nu a fost informat(ă) prin prezentul contract, ori pe care nu dorește să o achite;",
                "Dreptul de a fi sprijinit de un voluntar din cadrul asociației, cu atribuția de manager de caz, ale cărui nume și date de contact sunt precizate la punctul I;",
                "Dreptul de a renunța la sprijinul pe care l-a solicitat/l-a primit, fără nicio justificare, printr-o cerere scrisă, transmisă (prin mesaj telefonic) managerului de caz cu care a comunicat;",
                "Obligația de a respecta valorile și identitatea A.O.D.B. și de a nu denigra bunul nume al A.O.D.B.;",
                "Obligația de a discuta orice nemultumire legată de desfășurarea procesului de asistare, mai întâi cu reprezentanții A.O.D.B., în vederea soluționării lor;",
                "Dreptul de a susține activitățile Centrului EMAUS, prin donații benevole, oferite fie reprezentanților A.O.D.B. (cu primirea unei chitanțe de donație), fie prin virament bancar în contul asociației, menționat la punctul I;",
                "Beneficiarul înțelege și este de acord cu dreptul A.O.D.B. de a înceta sprijinul, în urma unui anunț prealabil, fără niciun fel de repercursiuni, ca urmare a intrării asociației, din orice motive, în imposibilitatea de a-și desfășura activitatea, respectiv a unor carențe grave de comunicare/colaborare cu acesta, în procesul de asistare;",
                "În cazul înrăutățirii stării de sănătate – până la lipsa propriei autonomii – ori a decesului beneficiarului în locuința Centrului EMAUS, persoana desemnată de către acesta la punctul I, ca având calitatea de persoană de sprijin, se va ocupa de toate demersurile necesare, A.O.D.B. neavând nicio responsabilitate suplimentară în acest sens – altele decât cele prevăzute de lege;",
                "În cazul în care beneficiarul ajunge în situația prevăzută la punctul anterior (V, lit. t) și nimeni nu-i acordă sprijin, A.O.D.B. va apela – fără nicio consultare prealabilă cu alți aparținători ai acestuia – la serviciile organelor statului abilitate să ia măsuri în astfel de situații;",
                "Beneficiarul exonerează A.O.D.B. de orice responsabilitate vis-a-vis de evoluția stării sale de sănătate pe perioada cazării în locuința oferită – inclusiv în contextul contractării unei afecțiuni virale – având obligația de a nu accepta cazarea/celelalte activități dacă presupune orice risc la adresa sănătății sale și asumându-și integral toate consecințele prin acceptarea acestora;");

            column.Item().PageBreak();

            Heading(column, "VI. Definirea termenilor");
            LetteredList(column,
                "Beneficiar = persoană fizică care se află în situație de dificultate, conform criteriilor de eligibilitate ale proiectului și constatate prin anchetă socială, care apelează la sprijinul A.O.D.B.;",
                "Proiect de asistență socială = totalitatea activităților concepute și implementate în vederea sprijinirii beneficiarului, pentru a depăși (social) situația de dificultate cu care se confruntă;",
                "Proces de asistare = implementarea activităților și acțiunilor prevăzute în proiectul de asistență socială, pentru a sprijini beneficiarul ca să depășească situația de dificultate cu care se confruntă, în limitele competențelor / atribuțiilor / abilităților voluntarilor implicați;",
                "Reprezentanți A.O.D.B. = orice persoană care a intrat în contact cu beneficiarul, la recomandarea și sub îndrumarea asistentului social A.O.D.B.;");

            Heading(column, "VII. Declarație:");
            Paragraph(column,
                "Subsemnatul, declar că – odată cu semnarea prezentului contract – mi-au fost prezentate " +
                "următoarele documente anexate, pe care le-am citit cu atenție și cu care sunt de acord întru totul:");
            LetteredList(column,
                "Criteriile de eligibilitate",
                "Regulamentul de ordine interioară al centrului EMAUS;",
                "Proces verbal al obiectelor de inventar din locuință;");

            Heading(column, "VIII. Dispoziţii finale");
            LetteredList(column,
                "Prezentul contract nu poate fi transmis unei terţe parti;",
                "Orice modificare a clauzelor contractului se va putea face numai cu acordul părţilor, prin acte adiţionale care vor face parte integrată din contract;",
                "Prezentul contract s-a redactat în două exemplare originale, câte unul pentru fiecare parte semnatară;",
                "Prezentul contract nu poate fi anulat prin decizia unilateriala a uneia dintre parti, cu exceptia situaților explicit prevăzute în cuprinsul său;",
                "Comunicarea între beneficiar și A.O.D.B. se face exclusiv prin numărul de telefon al asociației, menționat la punctul I;",
                "În caz de forță majoră – în special, în contextul evoluției pandemiei de COVID, dar nu numai – A.O.D.B. este exonerată de îndeplinirea atribuțiilor contractuale.");

            column.Item().PaddingTop(20).Text($"Localitatea: {Blank}     Data: {Blank}");

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text(AssociationName).Bold();
                    col.Item().AlignCenter().Text("București");
                    col.Item().AlignCenter().Text("Samuel Sandu");
                    col.Item().PaddingTop(30).AlignCenter().Text("(semnătură)").FontSize(8);
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text("Beneficiar proiect").Bold();
                    col.Item().AlignCenter().Text("Numele si prenumele");
                    col.Item().AlignCenter().Text(data.BeneficiaryFullName);
                    col.Item().PaddingTop(30).AlignCenter().Text("(semnătură)").FontSize(8);
                });
            });
        });
    }

    private static void Heading(ColumnDescriptor column, string text) =>
        column.Item().PaddingTop(4).Text(text).Bold().FontSize(10.5f);

    private static void SubHeading(ColumnDescriptor column, string text) =>
        column.Item().PaddingTop(2).AlignCenter().Text(text).Bold();

    private static void Paragraph(ColumnDescriptor column, string text) =>
        column.Item().Text(text).Justify();

    private static void LetteredList(ColumnDescriptor column, params string[] items)
    {
        var letter = 'a';
        foreach (var item in items)
        {
            column.Item().Text(t =>
            {
                t.Justify();
                t.Span($"{letter}. ");
                t.Span(item);
            });
            letter++;
        }
    }

    private static string OrBlank(string? value) => string.IsNullOrWhiteSpace(value) ? Blank : value;
}
