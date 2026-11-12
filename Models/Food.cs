using System.Text.Json.Serialization;
using SQLite;

namespace FoodOrderingApp.Models;

public class Food
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>Bundled photo in Resources/Images (e.g. "food_margherita.jpg").</summary>
    public string Image { get; set; } = string.Empty;

    public bool IsPopular { get; set; }

    // Display helper used by XAML bindings
    [JsonIgnore, Ignore] public string PriceText => $"${Price:F2}";
}
