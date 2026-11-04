using Grpc.Core;
using Microsoft.AspNetCore.Components;
using CampusStore.Protos;

namespace Day22Demo.Client.Components.Pages;

public partial class ProductStream : ComponentBase
{
    [Inject]
    private ProductCatalog.ProductCatalogClient ProductClient { get; set; } = default!;

    private List<Product> ProductList { get; set; } = [];
    private bool IsStreaming { get; set; }
    private string? ErrorMessage { get; set; }

    private CancellationTokenSource? _cts;

    private async Task LoadStreamAsync()
    {
        try
        {
            // prepare for the stream
            IsStreaming = true; // this updates UI
            ErrorMessage = null;
            ProductList.Clear();

            // replace the cts
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            StateHasChanged();

            // start stream
            using var call = ProductClient.StreamProducts(
                new StreamProductsRequest(),
                cancellationToken : _cts.Token         
                );
            await foreach (var product in call.ResponseStream.ReadAllAsync(_cts.Token))
            {
                ProductList.Add(product);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (RpcException ex)
        // for cancel
        when (
            ex.StatusCode == StatusCode.Cancelled 
            && _cts?.IsCancellationRequested == true
        ) { }

        catch (OperationCanceledException)
        when (_cts?.IsCancellationRequested ==true) { }

        catch (RpcException ex)
        {
            ErrorMessage = $"Streaming error: {ex.Status.Detail}";
        }
        finally { IsStreaming = false; }

    }

    private void StopStream()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
