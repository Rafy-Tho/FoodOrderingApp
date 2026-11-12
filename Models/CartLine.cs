using SQLite;

namespace FoodOrderingApp.Models;

/// <summary>One line of a user's cart (replaces users/&lt;id&gt;/cart.json).</summary>
public class CartLine
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string UserId { get; set; } = string.Empty;

    public int FoodId { get; set; }
    public int Quantity { get; set; }
}
