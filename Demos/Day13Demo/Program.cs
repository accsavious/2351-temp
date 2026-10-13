// core auth types (schemes, challenge async, signoutasync)
using Microsoft.AspNetCore.Authentication;
// cookie scheme: keeps user signed in between requests
using Microsoft.AspNetCore.Authentication.Cookies;
// OIDC scheme: talks to the id provider
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
// TokenValidationParameters
using Microsoft.IdentityModel.Tokens;


using Day13Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure authentication: Cookie for the session
builder.Services.AddAuthentication(options =>
{
    // Who is this?
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // login needed: send user to the provider
    options.DefaultChallengeScheme = "CampusHub";
}).AddCookie()
.AddOpenIdConnect("CampusHub", options =>
{
    // Provider base URL: endpoints come from its discovery document
    options.Authority = builder.Configuration["Oidc:Authority"];
    // Who this app is to the provider
    options.ClientId = builder.Configuration["Oidc:ClientId"];
    // From user.secrets, never put in your appsettings.json
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    // authorization code flow
    options.ResponseType = "code";

    // Request scopes
    // Drop the handlers defaults so the list below is the whole list
    options.Scope.Clear();
    // Required: make this OIDC and returns an id_token
    options.Scope.Add("openid");
    // Name claim
    options.Scope.Add("profile");
    // email claims
    options.Scope.Add("email");

    // Where the provider sends the code: must match the registered redirect url
    options.CallbackPath = "/callback";
    // No visible effect here: claims keep the tokens own issuer
    options.ClaimsIssuer = "CampusHub";
    // Keep id_token + access_token in the cookie for later use
    options.SaveTokens = true;
    // keep the claims names as sent: subject, name, email
    options.MapInboundClaims = false;
    // use the provider's name claim for User.Identity.Name
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // User.Identity.Name  read the "name" claim
        NameClaimType = "name"
    };

});

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

// First, read the cookie
app.UseAuthentication();
// Then: is the user allowed?
app.UseAuthorization();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login: redirect to the provider
app.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
// open re-direct guard: only follow a relative returnUrl like "/profile/"
// A returnUrl such as https//evil.com would otherwise send the user to an
// attacker-controlled site after login.
// Note: this simple check still allows "/evil.com"; production code should use
// URL.IsLocalUrl(returnUrl), which also rejects protocal-relative URLS.
{
    if(!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        // Not a relative url: fall back to home
        returnUrl = "/";
    }
    // start OIDC Flow
    await httpContext.ChallengeAsync("CampusHub", new AuthenticationProperties
    {
        RedirectUri = returnUrl
    });
});

app.MapGet("/Account/Logout", async (HttpContext httpContext) =>
{
    // Delete this apps auth cookie
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // then end the session at the provider
    await httpContext.SignOutAsync("CampusHub", new AuthenticationProperties
    {
        // Where to land after sign-out
        RedirectUri = "/"
    });
});



app.Run();
