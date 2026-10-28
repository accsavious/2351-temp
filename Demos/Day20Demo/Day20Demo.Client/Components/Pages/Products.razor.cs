using CampusStore.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Components;

namespace Day20Demo.Client.Components.Pages
{
    public partial class Products
    {
        // inject generated client that AddGrpcClient registered
        [Inject]
        private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;
        private List<Product> ProductList { get; set; } = [];
        private string? ErrorMessage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // A typed rpc call: no url or json
                var response = await ProductClient.ListProductsAsync(
                    new ListProductsRequest());
                // spread repeated field into a list for binding

                ProductList = [.. response.Products];
            }
            // every grpc failure arrives as one rpc exception
            catch (RpcException ex)
            {
                // map status code to friendly message
                ErrorMessage = ex.StatusCode switch
                {
                    StatusCode.Unavailable => "Product service is currently unavailable",
                    _ => $"Error: {ex.Status.Detail}"
                };
            }
        }
    }
}