using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day23Demo.Client.Components.Pages;

public partial class ProductLookup : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private int ProductId { get; set; } = 1;
    private Product? SelectedProduct { get; set; }
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private async Task LookupProduct()
    {
        ErrorMessage = null;
        SelectedProduct = null;
        IsLoading = true;

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(3);
            var response = await ProductClient.GetProductAsync(
                new GetProductRequest { Id = ProductId },
                // abort at deadline
                deadline: deadline
                );
            SelectedProduct = response.Product;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            ErrorMessage = $"Request timed out. Please try again";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            ErrorMessage = $"Product not found {ex.Status.Detail}";
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            ErrorMessage = $"Server is currently unavailable";
        }
        catch (RpcException ex)
        {
            ErrorMessage = $"Service Errpr: {ex.Status}: {ex.Status.Detail}";
        }

        finally
        {
            IsLoading = false;
        }
    }
}
