using System.Text.Json.Serialization;
using SQLite;

namespace FoodOrderingApp.Models;

public class OrderItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int OrderId { get; set; }

    public string FoodName { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore, Ignore] public decimal TotalPrice => UnitPrice * Quantity;
    [JsonIgnore, Ignore] public string TotalPriceText => $"${TotalPrice:F2}";
}
