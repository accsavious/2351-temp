using System.Security.Claims;
using CampusHub.AuthServer.Data;
using CampusHub.AuthServer.Diagnostics;
using CampusHub.AuthServer.Identity;
using CampusHub.AuthServer.Seed;
using Microsoft.AspNetCore;   // GOTCHA: GetOpenIddictServerRequest() moved to this namespace in 7.x
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Where the database lives
// ---------------------------------------------------------------------------
// Under the per-user local application data folder: %LOCALAPPDATA% on Windows,
// ~/.local/share on Linux. NOT the system temp folder — temp is swept by the OS on a
// schedule you do not control, and losing the token store in the middle of a lab looks
// exactly like a bug in the student's own code.
var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "CampusHub.AuthServer");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "authserver.sqlite3");

// `dotnet run -- --reset` throws the database away and reseeds from scratch. This is the ONLY
// path that deletes anything, and you have to ask for it by name.
if (args.Contains("--reset") && File.Exists(databasePath))
{
    File.Delete(databasePath);
    Console.WriteLine($"Reset: deleted {databasePath}");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlite($"Filename={databasePath}");
    options.UseOpenIddict();
});

builder.Services.AddRazorPages();

// Every OIDC step and every rejection reason, shown on https://localhost:7300/Activity as well
// as in the console. Same messages, easier to read than a scrolling window.
var activityLog = new ActivityLog();
builder.Services.AddSingleton(activityLog);
builder.Logging.AddProvider(new ActivityLoggerProvider(activityLog));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.Name = "CampusHub.AuthServer";
    });

builder.Services.AddOpenIddict()
    .AddCore(options => options
        .UseEntityFrameworkCore()
        .UseDbContext<AuthDbContext>())

    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               // Required for standard sign-out. Providers with a proprietary logout URL
               // make the client hardcode it; standard RP-initiated logout instead
               // advertises `end_session_endpoint` in the discovery document, which is
               // where the OIDC handler looks. Delete this and sign-out silently no-ops.
               .SetEndSessionEndpointUris("connect/logout");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange()   // PKCE genuinely enforced, not just advertised
               .AllowRefreshTokenFlow();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));
        options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(30));

        // GOTCHA: OpenIddict rotates refresh tokens but by default still accepts the previous
        // one for a 30-second leeway window (tolerance for concurrent requests). Zero makes
        // rotation strictly one-time-use, which is what makes it OBSERVABLE — reuse an old
        // refresh token and you get a real error instead of a silent success.
        options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);

        options.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email,
                               Scopes.OfflineAccess, Clients.ApiScope);

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // GOTCHA: OpenIddict encrypts access tokens by default, and an ordinary JwtBearer
        // resource server cannot read an encrypted token — it fails with a bare 401 that says
        // nothing useful. Disabling encryption issues plain, signed, validatable JWTs, which
        // is what every hosted provider hands you and what the gRPC server expects.
        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    });

builder.Services.AddAuthorization();
builder.Services.AddHostedService<SeedService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// ---------------------------------------------------------------------------
// /connect/authorize
// ---------------------------------------------------------------------------
app.MapMethods("/connect/authorize", ["GET", "POST"], async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("The OpenIddict request could not be retrieved.");

    // Not signed in yet? Bounce to the login page, remembering where we were headed.
    var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded)
    {
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl },
            [CookieAuthenticationDefaults.AuthenticationScheme]);
    }

    var email = result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var user = Fixtures.FindByEmail(email);
    if (user is null)
    {
        return Results.BadRequest(new { error = Errors.InvalidRequest, message = "Unknown user." });
    }

    var scopeManager = context.RequestServices.GetRequiredService<IOpenIddictScopeManager>();
    var identity = await ClaimsFactory.BuildAsync(
        user, request.ClientId, request.GetScopes(), scopeManager);

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

// ---------------------------------------------------------------------------
// /connect/token
// ---------------------------------------------------------------------------
app.MapPost("/connect/token", async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("The OpenIddict request could not be retrieved.");

    if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
    {
        return Results.BadRequest(new { error = Errors.UnsupportedGrantType });
    }

    var principal = (await context.AuthenticateAsync(
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

    // GOTCHA: the principal on this path carries `sub` — the opaque id — NOT the email. Look
    // the user up by subject or the refresh grant throws. It fails only on REFRESH, never on
    // first sign-in, which makes it a very easy bug to ship.
    var subject = principal.FindFirstValue(Claims.Subject)!;
    var user = Fixtures.FindBySubject(subject);
    if (user is null)
    {
        return Results.BadRequest(new { error = Errors.InvalidGrant, message = "Unknown subject." });
    }

    // Rebuild the identity from scratch so a refreshed token reflects the CURRENT role state
    // rather than replaying whatever was true when the user first signed in.
    var scopeManager = context.RequestServices.GetRequiredService<IOpenIddictScopeManager>();
    var identity = await ClaimsFactory.BuildAsync(
        user, request.ClientId, principal.GetScopes(), scopeManager);

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

// ---------------------------------------------------------------------------
// /connect/logout  (advertised as `end_session_endpoint` in the discovery document)
// ---------------------------------------------------------------------------
app.MapMethods("/connect/logout", ["GET", "POST"], async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Hands control back to OpenIddict, which validates post_logout_redirect_uri against the
    // client's registration before redirecting. That validation is the reason this is a
    // standard endpoint and not just a cookie delete.
    return Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
});

app.MapGet("/", () => Results.Redirect("/Login"));

app.Run();
