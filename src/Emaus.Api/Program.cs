using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emaus.Api.Middleware;
using Emaus.Api.Services.Auth;
using Emaus.Api.Services.Beneficiaries;
using Emaus.Api.Services.Bob;
using Emaus.Api.Services.Bookings;
using Emaus.Api.Services.Cleaning;
using Emaus.Api.Services.Logs;
using Emaus.Api.Services.Maintenance;
using Emaus.Api.Services.Menu;
using Emaus.Api.Services.Notifications;
using Emaus.Api.Services.Opportunities;
using Emaus.Api.Services.Properties;
using Emaus.Api.Services.Security;
using Emaus.Api.Services.Stats;
using Emaus.Api.Services.Users;
using Emaus.Domain.Repositories;
using Emaus.Infrastructure.Data;
using Emaus.Infrastructure.Repositories;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

// Gratuit pentru organizații/persoane cu venit anual sub 1.000.000 USD (licența Community) —
// Asociația Emaus se încadrează clar. Vezi https://www.questpdf.com/license/ dacă situația
// financiară a asociației se schimbă vreodată radical.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---- Configurare & DI ------------------------------------------------------------------

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Cache in-proces (single instance, dezvoltare) — folosit pentru date rar schimbate/scumpe de
// recalculat (ex. OrganizationSettings/BobSettings, un singur rând citit la fiecare dashboard).
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// ---- Repository / Unit of Work (generic — vezi Emaus.Domain/Repositories/IRepository.cs
// pentru raționamentul din spatele lui, și cele două excepții: IUserRepository/ILocalityRepository) ----
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILocalityRepository, LocalityRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---- Servicii (logica de business — controllerele rămân subțiri) -----------------------
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<BeneficiaryService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CleaningService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<OpportunityService>();
builder.Services.AddScoped<RequestLogService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<MenuService>();

// ---- Box of Blessing (BOB) — vezi Emaus.Domain/Bob, docs/API.md §13 --------------------
builder.Services.AddScoped<BobBeneficiaryService>();
builder.Services.AddScoped<BobBoxService>();
builder.Services.AddScoped<BobPurchaseService>();
builder.Services.AddScoped<BobStatsService>();

// ---- Push notifications (FCM) -----------------------------------------------------------
// Opțional: dacă `Firebase:ServiceAccountKeyPath` lipsește sau fișierul nu există (implicit
// în dezvoltare, până se configurează un cont Firebase real), aplicația pornește normal —
// notificările în-app tot funcționează, doar push-ul real e dezactivat (vezi
// NullPushNotificationSender). Nu tratăm lipsa Firebase ca eroare de configurare fatală.
var firebaseKeyPath = builder.Configuration["Firebase:ServiceAccountKeyPath"];
var pushEnabled = !string.IsNullOrWhiteSpace(firebaseKeyPath) && File.Exists(firebaseKeyPath);
if (pushEnabled)
{
    if (FirebaseApp.DefaultInstance is null)
    {
        // GoogleCredential.FromFile e marcat obsolete în Google.Apis.Auth mai nou (recomandă
        // CredentialFactory, o suprafață API diferită) — rămâne funcțional și e exact ce
        // documentează Firebase pentru citirea unui fișier JSON de cont de service; nu merită
        // migrat fără un motiv concret.
#pragma warning disable CS0618
        FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(firebaseKeyPath) });
#pragma warning restore CS0618
    }
    builder.Services.AddSingleton<IPushNotificationSender, FcmPushNotificationSender>();
}
else
{
    builder.Services.AddSingleton<IPushNotificationSender, NullPushNotificationSender>();
}

builder.Services.AddControllers()
    // Enum-urile (UnitStatus, BookingStatus etc.) ies ca text ("Available", nu 0) —
    // clientul React/React Native lucrează cu nume, nu cu numere greu de urmărit.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Emaus API", Version = "v1" });

    // Buton "Authorize" în Swagger UI, ca să poți testa endpoint-urile protejate direct din browser.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introdu doar token-ul primit de la /api/auth/login (fără cuvântul \"Bearer\")."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Secțiunea de configurare \"Jwt\" lipsește din appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };

        // Un token e valid din punct de vedere criptografic chiar și după ce contul din
        // spatele lui a dispărut — ex. în dezvoltare, când baza de date SQLite locală e
        // ștearsă/recreată (EnsureCreated seamănă mereu un Nucleus nou, cu alt Guid) în timp
        // ce telefonul/browserul mai are încă tokenul vechi salvat. Fără verificarea de mai
        // jos, orice cerere care scrie ceva (ex. crearea unei cazări, care leagă
        // Booking.CreatedByUserId de user) pică cu un 500 confuz — o eroare de FOREIGN KEY
        // ajunsă neprinsă până la ExceptionHandlingMiddleware, nu ceva legat de datele trimise.
        // Respingem explicit aici, cu un mesaj clar, cât mai devreme posibil în pipeline.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdRaw = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                var user = Guid.TryParse(userIdRaw, out var userId) ? await users.GetByIdAsync(userId) : null;
                if (user is null || !user.IsActive)
                {
                    context.Fail("Sesiunea nu mai este validă — te rugăm să te reconectezi.");
                }
            },
            OnChallenge = async context =>
            {
                // Challenge-ul implicit nu are corp — dar clientul (mobil/web) așteaptă mereu
                // { "error": "..." } ca la restul API-ului, altfel afișează un mesaj generic.
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Trebuie să fii autentificat, sau sesiunea ta nu mai este validă — te rugăm să te reconectezi."
                }));
            }
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
const string CorsPolicyName = "EmausClients";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

var app = builder.Build();

// ---- Bază de date: creare + seed la pornire (dezvoltare) -------------------------------
// Pentru producție pe SQL Server/PostgreSQL, înlocuiți EnsureCreated cu migrații EF Core
// reale (`dotnet ef migrations add InitialCreate`, apoi `Database.Migrate()`) — vezi
// backend/schema.md, secțiunea "Următorul pas tehnic".
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db, logger);

    if (!pushEnabled)
    {
        logger.LogWarning(
            "Push notifications (FCM) NU sunt configurate — setează Firebase:ServiceAccountKeyPath " +
            "în appsettings către fișierul JSON al contului de service Firebase. Notificările în-app " +
            "(GET /api/notifications/mine) funcționează normal indiferent de asta.");
    }
}

// ---- Middleware pipeline -----------------------------------------------------------------

// Cel mai din afară: cronometrează + jurnalizează FIECARE cerere (vezi RequestLog/LogsController),
// inclusiv statusul final scris mai jos de ExceptionHandlingMiddleware. Trebuie să rămână primul.
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
