using Day05Demo.Services;
using Fluxor;

namespace Day05Demo.Features.Product.Store;

// TODO (Demonstration, Step 4): THE NEW PIECE -- the effect.
// Give this class a primary constructor that injects IProductService:
//     public class ProductEffects(IProductService productService)
// Then write the [EffectMethod] async body below: inside a try/catch, await
// productService.GetAllAsync() and dispatch LoadProductsSuccessAction; in the
// catch, dispatch LoadProductsFailureAction(ex.Message).
// The placeholder body keeps the project compiling until the real code is typed.

public class ProductEffects(IProductService productService)
{
    [EffectMethod]
    public async Task HandleLoadProductsAction(LoadProductsAction action, IDispatcher dispatcher)
    {
        try
        {
            var products = await productService.GetAllAsync();
            dispatcher.Dispatch(new LoadProductsSuccessAction(products));
        }
        catch (Exception e)
        {
            dispatcher.Dispatch(new LoadProductsFailureAction(e.Message));
        }
    }
}
