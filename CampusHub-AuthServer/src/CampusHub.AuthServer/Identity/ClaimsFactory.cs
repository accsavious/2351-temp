using System.Collections.Immutable;
using System.Security.Claims;
using CampusHub.AuthServer.Seed;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CampusHub.AuthServer.Identity;

/// <summary>
/// Mints the claims that go into a token.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read this file.</b> Everything your application's authorization depends on is decided
/// here: which claims exist, what they are called, and which of the two tokens each one rides
/// in. On a hosted identity provider this logic exists too — as a JavaScript snippet in a
/// dashboard that you cannot read, diff, or step through in a debugger. Here it is ordinary
/// C# in a file you own.
/// </para>
/// </remarks>
public static class ClaimsFactory
{
    public const string PermissionsClaim = "permissions";

    public static async Task<ClaimsIdentity> BuildAsync(
        FixtureUser user,
        string? clientId,
        ImmutableArray<string> scopes,
        IOpenIddictScopeManager scopeManager)
    {
        var profile = Clients.ProfileFor(clientId);

        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: Claims.Name,
            roleType: profile.RolesClaimType);

        // `sub` is the STABLE, OPAQUE user id — never the email. Emails change; a subject
        // identifier must not, because everything downstream keys off it.
        identity.AddClaim(new Claim(Claims.Subject, user.Subject));
        identity.AddClaim(new Claim(Claims.Name, user.Email));
        identity.AddClaim(new Claim(Claims.Email, user.Email));

        // ORDER MATTERS: scopes must be set before anything reads them back, and the
        // permissions block below does exactly that. Setting them later emits no permissions
        // at all, silently.
        identity.SetScopes(scopes);

        // Each role is added as its OWN claim. Multiple claims of the same type are encoded in
        // the JWT as a JSON array, which the receiving handler expands back into N
        // single-valued claims — which is what RequireRole and HasClaim need.
        foreach (var role in user.Roles)
        {
            identity.AddClaim(new Claim(profile.RolesClaimType, role));
        }

        // A department is a PROPERTY of the user, not a group they belong to — which is exactly
        // why `[Authorize(Roles = ...)]` cannot express "is this user in Inventory?" and a
        // policy can. Emitted only for the applications whose claim profile asks for it: an
        // application that has no rule about departments has no business receiving one.
        if (profile.EmitDepartment && user.Department is not null)
        {
            identity.AddClaim(new Claim(Clients.DepartmentClaim, user.Department));
        }

        // Permissions are emitted ONLY when the API scope was actually granted. A token minted
        // for plain sign-in must not carry API authority — that is what a scope is FOR.
        //
        // GOTCHA: add each permission as a separate claim, exactly as above. Do NOT join them
        // into one space-delimited string (the way the `scope` claim is encoded). That arrives
        // as a single claim whose value is "records:read events:manage", and every
        // HasClaim(c => c.Value == "records:read") check returns false — with no error.
        if (identity.GetScopes().Contains(Clients.ApiScope))
        {
            foreach (var permission in Fixtures.PermissionsFor(user.Roles))
            {
                identity.AddClaim(new Claim(PermissionsClaim, permission));
            }
        }

        // GOTCHA: registering a scope is not enough — its Resources must be attached to the
        // identity explicitly, or the token carries no `aud` claim and the resource server
        // rejects it with "The audience 'empty' is invalid".
        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(identity.GetScopes()))
        {
            resources.Add(resource);
        }
        identity.SetResources(resources);

        identity.SetDestinations(claim => claim.Type switch
        {
            // The roles claim must reach BOTH tokens: the ID token so the web app can render a
            // role-aware UI, and the access token because that is what the gRPC server
            // validates. A provider that can only put custom claims in the ID token cannot
            // support this lab at all.
            var type when type == profile.RolesClaimType =>
                [Destinations.AccessToken, Destinations.IdentityToken],

            // Permissions are an API-authority statement, so they ride the ACCESS token only.
            // The ID token describes who the user is to the client app; the gRPC server never
            // sees it.
            PermissionsClaim => [Destinations.AccessToken],

            // The department claim MUST reach the ID token. Day 16's handler reads it from
            // `context.User`, which the web app builds from the ID token when the sign-in
            // cookie is created — an access-token-only destination would leave the handler
            // reading a claim that is not there, and it would deny in silence.
            Clients.DepartmentClaim => [Destinations.AccessToken, Destinations.IdentityToken],

            Claims.Name or Claims.Email or Claims.Subject =>
                [Destinations.AccessToken, Destinations.IdentityToken],

            _ => [Destinations.AccessToken]
        });

        return identity;
    }
}
