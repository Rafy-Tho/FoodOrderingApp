using System.Collections.ObjectModel;
using FoodOrderingApp.Models;

namespace FoodOrderingApp.Services;

/// <summary>
/// Orders for the signed-in user, stored in the Order and OrderItem tables.
/// </summary>
public static class OrderService
{
    public static ObservableCollection<Order> Orders { get; } = new();

    private static string? _userId;

    // ---------- Loading / saving ----------

    public static void LoadForCurrentUser()
    {
        Orders.Clear();
        _userId = AuthService.CurrentUser?.Id;

        if (_userId is null) return;

        // Newest first (Database.GetOrders already sorts).
        foreach (Order order in Database.GetOrders(_userId))
            Orders.Add(order);
    }

    /// <summary>Drops the in-memory list (used on sign out); the rows are kept.</summary>
    public static void ResetForLogout()
    {
        Orders.Clear();
        _userId = null;
    }

    // ---------- Create ----------

    /// <summary>Returns the new order, or null if the cart is empty / nobody is signed in.</summary>
    public static Order? PlaceOrder(string customerName, string phoneNumber,
                                    string deliveryAddress, string paymentMethod)
    {
        User? user = AuthService.CurrentUser;
        if (user is null || CartService.Items.Count == 0) return null;

        Order order = new()
        {
            Id = GetNextOrderId(),
            UserId = user.Id,
            CustomerName = customerName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            DeliveryAddress = deliveryAddress.Trim(),
            PaymentMethod = paymentMethod,
            Subtotal = CartService.GetSubtotal(),
            DeliveryFee = CartService.GetDeliveryFee(),
            Total = CartService.GetTotal(),
            OrderDate = DateTime.Now,
            Status = "Pending",
            Items = CartService.Items.Select(c => new OrderItem
            {
                FoodName = c.Food.Name,
                Image = c.Food.Image,
                UnitPrice = c.Food.Price,
                Quantity = c.Quantity
            }).ToList()
        };

        Database.InsertOrder(order);
        Orders.Insert(0, order);   // newest first

        CartService.ClearCart();   // also clears the saved cart
        return order;
    }

    // ---------- Update / delete ----------

    public static void UpdateStatus(Order order, string status)
    {
        order.Status = status;
        Database.UpdateOrder(order);
    }

    public static void DeleteOrder(Order order)
    {
        Orders.Remove(order);
        Database.DeleteOrder(order.Id);
    }

    // ---------- Helpers ----------

    /// <summary>Order numbers stay unique across all users.</summary>
    private static int GetNextOrderId() => Database.NextOrderId();
}
