using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CampusHub.AuthServer.Seed;

/// <summary>
/// What claim contract a given client application gets.
/// </summary>
/// <remarks>
/// <para>
/// This is the single most useful thing this server buys you pedagogically: ONE provider,
/// SEVERAL applications, each with its own claim contract. A hosted provider does exactly this
/// — the contract is just buried in a dashboard where nobody can read it. Here it is a record.
/// </para>
/// <para>
/// The roles claim TYPE is a namespaced URI rather than a plain <c>role</c>. That is not
/// decoration: a namespaced claim type cannot collide with a reserved JWT claim name, nor with
/// some other system's idea of what <c>role</c> means. Every production identity provider does
/// this, which is why we do too.
/// </para>
/// </remarks>
/// <param name="RolesClaimType">The claim type each of the user's roles is emitted under.</param>
/// <param name="EmitDepartment">
/// Whether this application receives the user's <c>department</c> claim at all. Only Day 16
/// asks a question that a department can answer, so only Day 16 gets one. A claim an
/// application has no use for is not free — it is user data handed to code that did not need
/// it, which is the whole argument for minimising a claim contract.
/// </param>
public record ClaimProfile(string RolesClaimType, bool EmitDepartment = false);

public static class Clients
{
    public const string ApiScope = "campushub.api";
    public const string ApiResource = "https://campushub-api";

    /// <summary>The namespace Module 2's lesson projects read roles from.</summary>
    /// <remarks>
    /// It must match <c>TokenValidationParameters.RoleClaimType</c> in the Day 13/16 projects
    /// exactly. Change one without the other and every role check silently denies — no error,
    /// just an empty page. That failure is the Day 16 lesson, so the string is deliberately
    /// declared once, here, where it is readable.
    /// </remarks>
    public const string LessonRolesClaim = "https://sdev2351.example.com/roles";

    public const string DepartmentClaim = "department";

    /// <summary>Per-client claim profiles, keyed by client id.</summary>
    /// <remarks>
    /// Three applications, one user directory, three different claim contracts. Lab 2 reads
    /// roles from its own API namespace; the lesson projects read them from the course
    /// namespace; only Day 16 receives a department. A hosted provider does exactly this — the
    /// contract is just spread across dashboard screens instead of sitting in one table.
    /// </remarks>
    public static readonly Dictionary<string, ClaimProfile> Profiles = new()
    {
        ["campushub-lab2"] = new ClaimProfile(RolesClaimType: $"{ApiResource}/roles"),
        ["day13-practice"] = new ClaimProfile(RolesClaimType: LessonRolesClaim),
        // Day 14 teaches route protection, not identity shape: its code reads `email` and
        // nothing else. It therefore takes the Day 13 contract unchanged — no roles question is
        // asked on Day 14, so no roles answer needs designing for it.
        ["day14-practice"] = new ClaimProfile(RolesClaimType: LessonRolesClaim),
        ["day16-demo"] = new ClaimProfile(RolesClaimType: LessonRolesClaim, EmitDepartment: true)
    };

    /// <summary>Falls back to the Lab 2 contract for any client without an explicit profile.</summary>
    public static ClaimProfile ProfileFor(string? clientId) =>
        clientId is not null && Profiles.TryGetValue(clientId, out var profile)
            ? profile
            : Profiles["campushub-lab2"];

    public static OpenIddictScopeDescriptor ApiScopeDescriptor() => new()
    {
        Name = ApiScope,
        DisplayName = "CampusHub gRPC services",
        // GOTCHA: registering the scope is NOT enough. A scope must declare a Resource, or
        // no `aud` claim is emitted and the resource server rejects the token with a bare
        // 401 "The audience 'empty' is invalid". This one line cost the spike two debug cycles.
        Resources = { ApiResource }
    };

    public static IEnumerable<OpenIddictApplicationDescriptor> All() =>
    [
        new()
        {
            ClientId = "campushub-lab2",
            ClientSecret = "lab2-secret",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "CampusHub Lab 2",
            // Lab 2's Web project sets CallbackPath = "/callback", not the "/signin-oidc"
            // default. A redirect URI mismatch is rejected by the provider, on purpose, and
            // the error you get back is `invalid_request` ("The specified 'redirect_uri' is
            // not valid for this client application", ID2043).
            RedirectUris = { new Uri("https://localhost:5001/callback") },
            // NOT the app root — a natural guess, because a proprietary logout endpoint
            // typically takes a `returnTo` parameter pointing straight at the app. Those are
            // two different values: standard RP-initiated logout takes the OIDC handler's own
            // SignedOutCallbackPath, and the handler redirects to the app from there.
            // Register the app root and the provider answers the sign-out request with
            // 400 invalid_request — measured, not assumed (Session 631).
            PostLogoutRedirectUris = { new Uri("https://localhost:5001/signout-callback-oidc") },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + ApiScope
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        },

        // -------------------------------------------------------------------------------
        // Day 13 — "register your application with a provider"
        // -------------------------------------------------------------------------------
        // ONE registration covering two projects: the instructor demo (:5132) and the student
        // practice app (:5131). Both are the same application wearing two ports, which is also
        // why a real registration accepts a LIST of redirect URIs rather than one.
        new()
        {
            ClientId = "day13-practice",
            ClientSecret = "day13-secret",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Day 13 — OIDC Practice",
            RedirectUris =
            {
                new Uri("https://localhost:5131/callback"),   // Day13Practice
                new Uri("https://localhost:5132/callback")    // Day13Demo
            },
            // NOT the app root. The OIDC handler sends `post_logout_redirect_uri` pointing at
            // its own SignedOutCallbackPath (default /signout-callback-oidc), lands there, and
            // only THEN redirects to wherever SignOutAsync was told to go. Register the app
            // root instead and sign-out fails validation at the provider.
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:5131/signout-callback-oidc"),
                new Uri("https://localhost:5132/signout-callback-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile
                // No `campushub.api` and no refresh-token grant: this app signs a user in and
                // reads the claims that come back. It never calls an API on their behalf, so it
                // is not granted the authority to. Compare campushub-lab2 above.
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        },

        // -------------------------------------------------------------------------------
        // Day 14 — protected routes and the [Authorize] attribute
        // -------------------------------------------------------------------------------
        // Deliberately a near-copy of day13-practice, because Day 14 asks nothing new of the
        // provider: the day's subject is what the APPLICATION does with an identity it already
        // knows how to obtain. A separate registration rather than extra redirect URIs on
        // day13-practice, because two applications really are two applications — each gets its
        // own client id and secret, which is the registration boundary a real provider draws too.
        new()
        {
            ClientId = "day14-practice",
            ClientSecret = "day14-secret",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Day 14 — Protected Routes",
            RedirectUris =
            {
                new Uri("https://localhost:5141/callback"),   // Day14Practice
                new Uri("https://localhost:5142/callback")    // Day14Demo
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:5141/signout-callback-oidc"),
                new Uri("https://localhost:5142/signout-callback-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        },

        // -------------------------------------------------------------------------------
        // Day 16 — roles and custom policies
        // -------------------------------------------------------------------------------
        // Same shape as Day 13; the ONLY difference that matters is invisible here — its claim
        // profile (above) emits roles under the course namespace and adds `department`.
        new()
        {
            ClientId = "day16-demo",
            ClientSecret = "day16-secret",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Day 16 — Roles and Policies",
            RedirectUris =
            {
                new Uri("https://localhost:5161/callback"),   // Day16Demo
                new Uri("https://localhost:5162/callback")    // Day16Practice
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:5161/signout-callback-oidc"),
                new Uri("https://localhost:5162/signout-callback-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        }
    ];
}
