using Microsoft.AspNetCore.Authentication;                // core auth types (schemes, ChallengeAsync, SignOutAsync)
using Microsoft.AspNetCore.Authentication.Cookies;        // cookie scheme: keeps the user signed in between requests
using Microsoft.AspNetCore.Authentication.OpenIdConnect;  // OIDC scheme: talks to the identity provider
using Microsoft.IdentityModel.Tokens;                     // TokenValidationParameters (how tokens are checked)
using Day13Practice.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// [PRE-BUILT] OIDC authentication is already configured.
// Review this code to understand how it works (Practice 1).
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;  // every request: read "who is this?" from the auth cookie
    options.DefaultChallengeScheme = "CampusHub";  // login needed: send the user to the provider
})
.AddCookie()                               // after login, the session lives in an encrypted cookie
.AddOpenIdConnect("CampusHub", options =>  // "CampusHub" = this scheme's name, used above
{
    options.Authority = builder.Configuration["Oidc:Authority"];        // provider base URL; endpoints come from its discovery document
    options.ClientId = builder.Configuration["Oidc:ClientId"];          // who this app is to the provider
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];  // from user-secrets, never appsettings.json
    options.ResponseType = "code";                                      // Authorization Code Flow

    options.Scope.Clear();         // drop the handler's defaults so the list below is the whole list
    options.Scope.Add("openid");   // required: makes this OIDC and returns an id_token
    options.Scope.Add("profile");  // name claims
    options.Scope.Add("email");    // email claim

    options.CallbackPath = "/callback";  // where the provider sends the code; must match the registered redirect URI
    options.ClaimsIssuer = "CampusHub";  // no visible effect here: claims keep the token's own issuer
    options.SaveTokens = true;           // keep id_token + access_token in the cookie for later use
    // Keep the provider's claim names (sub, email) instead of renaming them to long
    // Microsoft URIs -- without this line, User.FindFirst("email") returns null.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name"  // User.Identity.Name reads the "name" claim
    };
});

// Lets components receive the signed-in user's AuthenticationState
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

app.UseAuthentication();  // first: read the cookie, who is this user?
app.UseAuthorization();   // then: is this user allowed? (must come after)

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login: the nav's Login link calls this, and it redirects to the provider
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

    // Start the OIDC flow: redirect to the provider's login page
    await httpContext.ChallengeAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = returnUrl  // where to land after login succeeds
    });
});

// Logout: sign out of the cookie and the provider
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
