using System.Text.Json.Serialization;
using SQLite;

namespace FoodOrderingApp.Models;

/// <summary>A filter chip on the home page: a name plus a Font Awesome icon.</summary>
public class FoodCategory
{
    public FoodCategory()
    {
    }

    public FoodCategory(string name, string iconKey)
    {
        Name = name;
        IconKey = iconKey;
    }

    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human readable icon name stored in the seed categories.json, e.g. "pizza".
    /// Mapped to a Font Awesome glyph by <see cref="CategoryIcons"/>.
    /// </summary>
    public string IconKey { get; set; } = "utensils";

    /// <summary>How many menu items use this category (admin dashboard only).</summary>
    [JsonIgnore, Ignore]
    public int ItemCount { get; set; }

    [JsonIgnore, Ignore] public string ItemCountText => ItemCount == 1 ? "1 item" : $"{ItemCount} items";

    [JsonIgnore, Ignore]
    public string Icon => CategoryIcons.Resolve(IconKey);
}
