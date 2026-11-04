using CampusStore.Protos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddGrpcClient<OrderManagement.OrderManagementClient>(
    options =>
    {
        options.Address = new Uri("https://localhost:7003");
    });

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Day20Practice.Client.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
