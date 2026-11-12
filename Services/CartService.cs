using System.Collections.ObjectModel;
using FoodOrderingApp.Models;

namespace FoodOrderingApp.Services;

/// <summary>
/// The signed-in user's cart, stored in the CartLine table after every change.
/// </summary>
public static class CartService
{
    public const decimal DeliveryFeeAmount = 2.50m;

    public static ObservableCollection<CartItem> Items { get; } = new();

    private static string? _userId;

    // ---------- Loading / saving ----------

    public static void LoadForCurrentUser()
    {
        Items.Clear();
        _userId = AuthService.CurrentUser?.Id;

        if (_userId is null) return;

        foreach (CartLine line in Database.GetCart(_userId))
        {
            Food? food = MenuService.GetById(line.FoodId);
            if (food is null) continue;              // dish was removed from the menu

            Items.Add(new CartItem(food, Math.Clamp(line.Quantity, 1, 99)));
        }
    }

    /// <summary>Drops the in-memory cart (used on sign out); the rows are kept.</summary>
    public static void ResetForLogout()
    {
        Items.Clear();
        _userId = null;
    }

    public static void Save()
    {
        if (_userId is null) return;

        Database.SaveCart(_userId, Items.Select(i => new CartLine
        {
            FoodId = i.Food.Id,
            Quantity = i.Quantity
        }));
    }

    // ---------- Mutations ----------

    public static void AddToCart(Food food)
    {
        CartItem? existing = Items.FirstOrDefault(c => c.Food.Id == food.Id);

        if (existing is not null)
            existing.Quantity = Math.Min(existing.Quantity + 1, 99);
        else
            Items.Add(new CartItem(food));

        Save();
    }

    public static void IncreaseQuantity(CartItem item)
    {
        if (item.Quantity < 99)
        {
            item.Quantity++;
            Save();
        }
    }

    public static void DecreaseQuantity(CartItem item)
    {
        if (item.Quantity > 1)   // minimum 1; use Remove to delete the row
        {
            item.Quantity--;
            Save();
        }
    }

    public static void RemoveFromCart(CartItem item)
    {
        Items.Remove(item);
        Save();
    }

    public static void ClearCart()
    {
        Items.Clear();
        Save();
    }

    // ---------- Totals ----------

    public static int GetCartItemCount() => Items.Sum(c => c.Quantity);

    public static decimal GetSubtotal() => Items.Sum(c => c.TotalPrice);

    // No delivery fee when the cart is empty
    public static decimal GetDeliveryFee() => Items.Count == 0 ? 0m : DeliveryFeeAmount;

    public static decimal GetTotal() => GetSubtotal() + GetDeliveryFee();
}
