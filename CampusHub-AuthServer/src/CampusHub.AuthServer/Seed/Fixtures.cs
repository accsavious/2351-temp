namespace CampusHub.AuthServer.Seed;

/// <summary>
/// A fixture user. <paramref name="Subject"/> is the stable, opaque identifier that becomes
/// the <c>sub</c> claim — deliberately NOT the email, because emails change and a subject
/// identifier must not.
/// </summary>
/// <remarks>
/// <paramref name="Department"/> is a plain attribute of the user, not a group they belong to,
/// and that distinction is the whole point of Day 16: a ROLE is membership ("is this user an
/// Admin?"), while a department is a property you have to reason about ("is this user in
/// Inventory?"). Roles are answered by <c>[Authorize(Roles = ...)]</c>; properties need a
/// policy. Most users have no department, so it is optional.
/// </remarks>
public record FixtureUser(string Subject, string Email, string[] Roles, string? Department = null);

/// <summary>
/// The in-memory user store. A real identity provider has a database and a password hasher;
/// this one has a dictionary, because the point of this server is to be READABLE. Everything
/// it does that matters — issuing tokens, minting claims, enforcing PKCE — is the real thing.
/// </summary>
public static class Fixtures
{
    /// <summary>
    /// One shared password for every fixture user. There are no real people behind these
    /// accounts and no real data behind the API, so a shared password is appropriate here and
    /// nowhere else. It is checked in ON PURPOSE: this server is a course fixture, not a
    /// deployment. Contrast <c>dotnet user-secrets</c>, which is how your CLIENT app stores
    /// its client secret — that one is a real secret and never belongs in the repo.
    /// </summary>
    public const string SharedPassword = "Campus#Hub2351";

    /// <summary>
    /// One roster, serving every application this provider knows about. The <c>user-0NN</c> block
    /// is the Lab 2 roster: one user per role, one user holding every role, and one user holding
    /// none. The <c>user-1NN</c> block serves the Day 13 and Day 16 practice applications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The no-role user is the important one — it is the only way to SEE that your authorization
    /// actually blocks unauthorized requests, rather than assuming it does from reading the code.
    /// It is shared by every application, Lab 2 and Day 16 alike.
    /// </para>
    /// <para>
    /// The subject values are not arbitrary. <c>user-001</c>, <c>user-002</c> and <c>user-003</c>
    /// are the seeded <c>StudentRecord.StudentId</c> and <c>Notification.UserId</c> values in
    /// Lab 2's <c>CampusHubDbContext</c>. Aligning them makes per-record OWNERSHIP checks
    /// ("does this record belong to the caller?") demonstrable — which was impossible when the
    /// subject was a hosted provider's opaque id that matched nothing in the seed data.
    /// </para>
    /// <para>
    /// One roster rather than one per client, deliberately. A real provider has a single user
    /// directory and issues DIFFERENT claims about the same person to different applications —
    /// which is exactly what <see cref="ClaimProfile"/> does. Splitting the roster per client
    /// would hide that, and it is the most transferable idea in this server.
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyList<FixtureUser> Users =
    [
        // Lab 2 — the six-role authorization model.
        new("user-001", "records@sdev2351.local",          ["RecordsAccess"]),
        new("user-002", "notifications@sdev2351.local",    ["NotificationAccess"]),
        new("user-003", "course-admin@sdev2351.local",     ["CourseAdmin"]),
        new("user-004", "library-staff@sdev2351.local",    ["LibraryStaff"]),
        new("user-005", "event-organizer@sdev2351.local",  ["EventOrganizer"]),
        new("user-006", "facility-manager@sdev2351.local", ["FacilityManager"]),
        new("user-007", "multi-role@sdev2351.local",       ["CourseAdmin", "LibraryStaff",
                                                            "FacilityManager", "EventOrganizer",
                                                            "RecordsAccess", "NotificationAccess"]),
        // Shared deny path — used by Lab 2 AND by Day 16.
        new("user-008", "no-role@sdev2351.local",          []),

        // Day 13 — sign-in only. No roles on purpose: Day 13 is about the OIDC handshake and
        // the claims that come back from it, not about authorization decisions.
        new("user-101", "demo@sdev2351.local",             []),

        // Day 16 — roles AND a department, because the day teaches the difference.
        // admin@ satisfies CanViewReports by ROLE; analyst@ satisfies the same policy by
        // DEPARTMENT; manager@ satisfies neither and is the negative case that proves the
        // handler's OR logic is actually running.
        //
        // analyst@ holds Manager for one reason, and it is not that an analyst is a manager:
        // CanViewReports is checked INSIDE /dashboard, which is itself [Authorize(Roles =
        // "Admin,Manager")]. Without a role that reaches the page, the department branch of
        // the OR could never be seen — it would be demonstrated in code and never on screen.
        // Manager is the role that grants ARRIVAL; Analytics is what grants the reports. That
        // separation is the point: analyst@ and manager@ reach the same page by the same
        // route and see different things, which is only explicable by the department.
        // (James's call, 2026-07-28. Fixture users are in-memory — no `--reset` needed.)
        new("user-102", "admin@sdev2351.local",            ["Admin"],   Department: "Inventory"),
        new("user-103", "manager@sdev2351.local",          ["Manager"], Department: "Inventory"),
        new("user-104", "analyst@sdev2351.local",          ["Manager"], Department: "Analytics")
    ];

    /// <summary>
    /// Permissions are granted to ROLES; a user inherits the union of the permissions of every
    /// role they hold. This mirrors how hosted providers model RBAC, and the two permission
    /// strings below are exactly the ones Lab 2's authorization handlers check for.
    /// </summary>
    static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["RecordsAccess"] = ["records:read"],
        ["EventOrganizer"] = ["events:manage"]
    };

    public static FixtureUser? FindByEmail(string email) =>
        Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reverse lookup by opaque subject id. The refresh-token grant rebuilds the identity from
    /// the incoming principal, which carries <c>sub</c> — not the email. Without this the
    /// refresh path throws, and it throws only on REFRESH, never on first sign-in, which makes
    /// it an easy bug to ship.
    /// </summary>
    public static FixtureUser? FindBySubject(string subject) =>
        Users.FirstOrDefault(u => u.Subject == subject);

    public static IEnumerable<string> PermissionsFor(IEnumerable<string> roles) =>
        roles.SelectMany(role => RolePermissions.TryGetValue(role, out var permissions)
                ? permissions
                : [])
             .Distinct()
             .Order();
}
