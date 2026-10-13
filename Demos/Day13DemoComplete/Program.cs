using Microsoft.AspNetCore.Authentication;                // core auth types (schemes, ChallengeAsync, SignOutAsync)
using Microsoft.AspNetCore.Authentication.Cookies;        // cookie scheme: keeps the user signed in between requests
using Microsoft.AspNetCore.Authentication.OpenIdConnect;  // OIDC scheme: talks to the identity provider
using Microsoft.IdentityModel.Tokens;                     // TokenValidationParameters (how tokens are checked)
using Day13Demo.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Two cooperating schemes, each with a different job:
//   DefaultScheme = Cookie    -> holds the session AFTER login, so the user isn't
//                                re-challenged on every request.
//   DefaultChallengeScheme    -> CampusHub (OIDC), the handler that runs WHEN a login
//                                is needed and redirects the browser to the provider.
// This is why both AddCookie() and AddOpenIdConnect() are registered below.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;  // every request: read "who is this?" from the auth cookie
    options.DefaultChallengeScheme = "CampusHub";  // login needed: send the user to the provider
})
.AddCookie()                               // after login, the session lives in an encrypted cookie
.AddOpenIdConnect("CampusHub", options =>  // "CampusHub" = this scheme's name, used above
{
    // "CampusHub" is the scheme name referenced later by ChallengeAsync / SignOutAsync.
    // It is a name YOU choose -- it is not magic and it is not the provider's name.
    // Authority is the provider's base URL; the middleware auto-discovers every OIDC
    // endpoint from {Authority}/.well-known/openid-configuration -- no endpoints
    // hardcoded. Open that URL in a browser to read what it finds. (It is fetched on
    // the first login, not at startup.)
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.ClientId = builder.Configuration["Oidc:ClientId"];  // who this app is to the provider
    // ClientId is public (appsettings.json); the secret is NEVER in the repo --
    // it's set via `dotnet user-secrets set "Oidc:ClientSecret" "<value>"`.
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    // "code" = Authorization Code Flow -- the same flow traced on paper in Day 12.
    // The server (not the browser) exchanges the code for tokens.
    options.ResponseType = "code";

    // Clear-then-add defines the EXACT scope set rather than appending to the
    // middleware's defaults: if a future .NET version changes those defaults, this
    // app still requests precisely what's listed. On most providers profile and email
    // unlock the name and email claims; ours sends sub, name and email whatever you ask
    // for (see the AuthServer's Identity/ClaimsFactory.cs).
    options.Scope.Clear();
    options.Scope.Add("openid");   // required: makes this OIDC and returns an id_token
    options.Scope.Add("profile");  // name claims
    options.Scope.Add("email");    // email claim

    // CallbackPath MUST match a redirect URI the provider has registered for this
    // client -- ours are declared in the AuthServer's Seed/Clients.cs, which you can
    // open and read. A mismatch is rejected by the provider on purpose, and it says
    // so: the browser stops on an `invalid_request` page ("The specified 'redirect_uri'
    // is not valid for this client application"), and the AuthServer console names the
    // rejected URI.
    options.CallbackPath = "/callback";
    // No visible effect in this app: every claim keeps the token's own issuer, which is
    // why the Profile page's Issuer column shows https://localhost:7300/.
    options.ClaimsIssuer = "CampusHub";
    // SaveTokens keeps the ID + access tokens in the auth ticket; with a bare
    // .AddCookie() that ticket is encrypted into the cookie itself. Nothing reads them
    // today -- an app that calls an API with the user's access token needs them
    // (Lab 2's AuthStateTokenProvider does).
    options.SaveTokens = true;
    // Keep the provider's claim names (sub, email) instead of renaming them to long
    // Microsoft URIs -- without this line, User.FindFirst("email") returns null.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Map the provider's "name" claim onto User.Identity.Name -- without this the
        // display name comes back null and the nav greeting is blank.
        NameClaimType = "name"
    };
});

// Makes the user's auth state available to every component as a [CascadingParameter]
// -- the cascading-parameter idea from SDEV 2301, carrying "who is this user / what
// claims do they have". (The <CascadingAuthenticationState> wrapper in Routes.razor
// supplies the same value, so either one alone is enough.)
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Order matters: UseAuthentication runs FIRST (reads the cookie, establishes WHO
// the user is), then UseAuthorization checks whether that user MAY access the
// resource. Reversed, the authorization check sees an anonymous user. Nothing is
// protected yet today, so you would see no difference; once Day 14 adds [Authorize],
// a reversed order sends a signed-in user back to the provider in a loop.
app.UseAuthentication();
app.UseAuthorization();  // then: is this user allowed? (must come after)

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login/Logout are minimal-API endpoints, NOT Blazor component code, on purpose:
// ChallengeAsync needs the raw HTTP request and response -- it writes the 302 to the
// provider and sets the correlation and nonce cookies on it. An endpoint gets the
// HttpContext directly. These pages are statically rendered (no SignalR connection);
// an interactive component has no HTTP response to write at all, so the endpoint is
// the one place that works in both.
app.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
{
    // Open-redirect guard: only follow a relative returnUrl like "/profile".
    // A returnUrl such as https://evil.com would otherwise send the user to an
    // attacker-controlled site after login.
    // NOTE: this simple check still allows "//evil.com"; production code should use
    // Url.IsLocalUrl(returnUrl), which also rejects protocol-relative URLs.
    if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";  // not a relative URL: fall back to home
    }

    // ChallengeAsync("CampusHub") hands off to the OIDC handler registered under that
    // scheme name, which generates the PKCE verifier/challenge, builds the authorize
    // URL from the discovery document, and redirects.
    await httpContext.ChallengeAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = returnUrl  // where to land after login succeeds
    });
});

// Logout signs out of BOTH sessions. Clearing only the local cookie leaves the user
// still logged in at the provider, so the next "Login" silently re-authenticates them
// with no prompt -- confusing behaviour. Sign out of the cookie AND the provider.
app.MapGet("/Account/Logout", async (HttpContext httpContext) =>
{
    // Delete this app's auth cookie
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // Then end the session at the provider
    await httpContext.SignOutAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = "/"  // where to land after sign-out
    });
});

app.Run();
