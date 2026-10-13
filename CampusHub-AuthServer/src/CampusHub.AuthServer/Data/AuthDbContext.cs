using Microsoft.EntityFrameworkCore;

namespace CampusHub.AuthServer.Data;

/// <summary>
/// Storage for OpenIddict's own tables (applications, scopes, authorizations, tokens).
/// It holds no user data — the fixture roster lives in code, in <c>Seed/Fixtures.cs</c>.
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options);
