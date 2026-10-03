using Blazored.LocalStorage;
using Day08Demo.Models;
using Fluxor;

namespace Day08Demo.Features.Cart.Store;

public class CartPersistenceEffects(
    ILocalStorageService localStorage,
    IState<CartState> cartState)
{
    private const string CartStorageKey = "cart";

    // The constructor injects two dependencies the effect methods will use:
    //   localStorage -- read/write the browser localStorage ("cart" key)
    //   cartState    -- cartState.Value.Items is the current cart after a reducer runs
    // These aliases keep the starter compiling cleanly until the effects are added;
    // the live-coded effects reference the constructor parameters directly.
    private ILocalStorageService LocalStorage => localStorage;
    private IState<CartState> CartState => cartState;

    // ============================================================
    // DEMONSTRATION Step 2 (CODE LIVE): Add the persistence effects
    //
    // TODO: Add an [EffectMethod] for AddToCartAction
    //       Save cartState.Value.Items to localStorage with key CartStorageKey
    //
    // TODO: Add an [EffectMethod] for RemoveFromCartAction
    //       Save cartState.Value.Items to localStorage with key CartStorageKey
    //
    // TODO: Add an [EffectMethod] for ClearCartAction
    //       Remove the CartStorageKey key from localStorage
    // ============================================================
    [EffectMethod]
    public async Task HandleAddToCartAction(AddToCartAction action, IDispatcher dispatcher)
    {
        // runs after the reducer stat is already updated
        await localStorage.SetItemAsync(CartStorageKey, cartState.Value.Items);
    }

    [EffectMethod]
    public async Task HandleRemoveFromCartAction(RemoveFromCartAction action, IDispatcher dispatcher)
    {
        await localStorage.SetItemAsync(CartStorageKey, cartState.Value.Items);
    }

    [EffectMethod]
    public async Task HandleClearCartAction(ClearCartAction action, IDispatcher dispatcher)
    {
        await localStorage.RemoveItemAsync(CartStorageKey);
    }

    // ============================================================
    // DEMONSTRATION Step 5 (CODE LIVE): Add the hydration effect
    //
    // TODO: Add an [EffectMethod] for HydrateCartRequestAction
    //       Try to load the cart with GetItemAsync<List<CartItem>>(CartStorageKey)
    //       If items found, dispatch HydrateCartAction(items)
    //       If GetItemAsync throws, remove the corrupt key and start fresh
    // ============================================================
    [EffectMethod]
    public async Task HandleHydrateCartRequestAction(HydrateCartRequestAction action, IDispatcher dispatcher)
    {
        try
        {
            List<CartItem>? items = await localStorage.GetItemAsync<List<CartItem>>(CartStorageKey);
            if (items is not null && items.Count > 0) {
                dispatcher.Dispatch(new HydrateCartAction(items));
            }
        }
        catch (Exception ex)
        {
            await localStorage.RemoveItemAsync(CartStorageKey);
        }
    }
}
