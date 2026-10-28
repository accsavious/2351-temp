// import the protos file
using CampusStore.Protos;
using Day20Demo.Client.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the token provider. AddCallCredentials resolves on every call
builder.Services.AddScoped<ITokenProvider, DemoTokenProvider>();

// register generated client
builder.Services.AddGrpcClient<ProductCatalog.ProductCatalogClient>(
    options =>
    {
        options.Address = new Uri("https://localhost:7001");
    })
    // Add call credential, running once per call, so a refresh toke is picked up
    .AddCallCredentials(async (context, metatdata, serviceProvider) =>
    {
        // Resolve the token provider from DI
        var tokenProvider = serviceProvider.GetRequiredService<ITokenProvider>();
        var token = await tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            metatdata.Add("Authorization", $"Bearer {token}");
        }
    })
    ;

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Day20Demo.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
