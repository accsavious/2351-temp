using CampusHub.AuthServer.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace CampusHub.AuthServer.Seed;

/// <summary>
/// Creates the database on first run and registers the scope and client applications.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately IDEMPOTENT: it checks for each object before creating it, so starting the
/// server twice is harmless. There is no <c>EnsureDeletedAsync</c> here. A throwaway probe can
/// drop its database on every boot; a server a student has been running for three weeks must
/// not, because the tokens and authorizations it holds are live. Resetting is an explicit,
/// opt-in act: <c>dotnet run -- --reset</c>.
/// </para>
/// <para>
/// <c>EnsureCreatedAsync</c> rather than EF migrations, on purpose. Migrations are a versioning
/// tool for a schema that evolves; this schema is OpenIddict's and it does not change between
/// terms. Adding a migrations folder here would be ceremony students would have to run and
/// would learn nothing from.
/// </para>
/// </remarks>
public class SeedService(IServiceProvider provider, ILogger<SeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var scopes = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        if (await scopes.FindByNameAsync(Clients.ApiScope, cancellationToken) is null)
        {
            await scopes.CreateAsync(Clients.ApiScopeDescriptor(), cancellationToken);
            logger.LogInformation("Seeded scope {Scope}", Clients.ApiScope);
        }

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        foreach (var descriptor in Clients.All())
        {
            if (await applications.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is null)
            {
                await applications.CreateAsync(descriptor, cancellationToken);
                logger.LogInformation("Seeded client {ClientId}", descriptor.ClientId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
