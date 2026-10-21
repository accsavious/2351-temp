using Day19Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// add gRPC framework for DI
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
var app = builder.Build();

// Route incoming gRPC calls to ProductCatalogService

app.MapGrpcService<ProductCatalogService>();
app.MapGrpcReflectionService();
app.Run();
