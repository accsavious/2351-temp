using Grpc.Core;
using CampusStore.Protos;


namespace Day19Demo.Services;

public class ProductCatalogService : ProductCatelog.ProductCatelogBase
{
    private static readonly List<Product> Products = [
        new() {Id = 1, Name = "C# in Depth", Price = 50, Category = ProductCategory.Books},
        new() {Id = 2, Name = "Mactop", Price = 900, Category = ProductCategory.Electronics},
        new() {Id = 2, Name = "Hoodie", Price =  45, Category = ProductCategory.Clothing},
    ];
    public override Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var product = Products.FirstOrDefault(p => p.Id == request.Id) 
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));

        return Task.FromResult(new GetProductResponse { Product = product });
    }

    public override Task<ListProductsResponse> ListProducts(ListProductsRequest request, ServerCallContext context)
    {
        var filtered = Products.AsEnumerable();

        // Unspecified (the enum's has a 0) means the client sent no filder
        if (request.Category != ProductCategory.Unspecified)
        {
            filtered = filtered.Where(p => p.Category == request.Category);
        }
        var response = new ListProductsResponse();
        // Its a repeated field, its a read-only list
        response.Products.AddRange(filtered);
        return Task.FromResult(response);
    }

}
