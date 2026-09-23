using Day07Demo.Features.Cart.Store;
namespace Day07Demo.Guards
{
    public static class CartGuard
    {
        public static bool ShouldRedirect(CartState state)
            => state.Items.Count == 0;
    }
}
