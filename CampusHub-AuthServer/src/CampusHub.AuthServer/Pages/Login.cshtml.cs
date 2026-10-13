using System.Security.Claims;
using CampusHub.AuthServer.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.AuthServer.Pages;

/// <summary>
/// The provider's hosted login UI. A hosted identity provider shows you its own page here too —
/// the difference is that this one is a file you can open.
/// </summary>
public class LoginModel(ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var user = Fixtures.FindByEmail(Email);

        // One message for both "no such user" and "wrong password", on purpose. Telling an
        // attacker which of the two they got right hands them a way to enumerate valid
        // accounts. Every real provider's login page behaves this way.
        if (user is null || Password != Fixtures.SharedPassword)
        {
            // The page stays vague for the attacker's sake; the log can be specific, because only
            // the person running the server reads it.
            logger.LogWarning("Sign-in failed for {Email}: {Reason}.", Email,
                user is null ? "no fixture account has that email" : "wrong password");
            ErrorMessage = "That email and password combination was not recognised.";
            return Page();
        }

        logger.LogInformation("Sign-in succeeded for {Email}. Returning to the authorization request.", user.Email);

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Email));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // The authorize endpoint sent us here with the original request in returnUrl; going
        // back to it resumes the interrupted authorization flow.
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/Login" : returnUrl);
    }
}
