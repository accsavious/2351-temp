using Microsoft.EntityFrameworkCore;
using Day26Demo.Data;
using Day26Demo.Models;
using BYSResults;

namespace Day26Demo.Services;

public class CheckoutService(StoreDbContext context)
{
    private readonly StoreDbContext _context = context;

    // Each SaveChangesAsync commits on its own
    public async Task<Result<string>> BrokenCheckoutAsync(int productId, int quantity, string customer)
    {
        // Load the product by its key
        var product = await _context.Products.FindAsync(productId);
        // No such produce: handle failure
        if (product is null) return Result<string>.Failure(new Error("Not Found", "Product not found"));



        // Not enough stock: fail before any data changes
        if (product.StockQuantity < quantity) return Result<string>.Failure(new Error("Validation", "Insufficent stock"));
        
        // Step 1: deduct inventory and save
        product.StockQuantity -= quantity;
           
        await _context.SaveChangesAsync();
        // we never make it to step 2
        throw new InvalidOperationException("Simulated failure");

        // this is where we should create an order, but we never reach it
    }

    public async Task<Result<string>> SafeCheckoutAsync(int productId, int quantity, string customer)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var product = await _context.Products.FindAsync(productId);
            // No such produce: handle failure
            if (product is null) return Result<string>.Failure(new Error("Not Found", "Product not found"));

            // Not enough stock: fail before any data changes
            if (product.StockQuantity < quantity) return Result<string>.Failure(new Error("Validation", "Insufficent stock"));

            // Step 1: deduct inventory
            product.StockQuantity -= quantity;

            // step 2
            var order = new Order
            {
                CustomerName = customer,
                ProductId = productId,
                Quantity = quantity,
                Total = product.Price * quantity,
                CreatedAt = DateTime.UtcNow
            };

            // Track new order
            _context.Orders.Add(order);

            // save
            await _context.SaveChangesAsync();

            // then commit 
            await transaction.CommitAsync();
            
            return Result<string>.Success($"Order #{order.Id} created for {customer}. Total: {order.Total:C}");
        }
        catch (Exception ex)
        {
            // Undo everything
            await transaction.RollbackAsync();
            // Drop the tracked entities so no stale stock value lingers
            _context.ChangeTracker.Clear();
            return Result<string>.Failure(new Error("Internal", $"Checkout failed: {ex.Message}"));
        }



    }

    /// <summary>
    /// [PRE-BUILT] Resets the database to its original seeded state.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        _context.Orders.RemoveRange(_context.Orders);

        var products = await _context.Products.ToListAsync();
        var seedData = new Dictionary<int, int>
        {
            { 1, 10 }, { 2, 50 }, { 3, 30 }, { 4, 15 }, { 5, 25 }
        };
        foreach (var product in products)
        {
            if (seedData.TryGetValue(product.Id, out int originalStock))
                product.StockQuantity = originalStock;
        }
        await _context.SaveChangesAsync();
    }
}
