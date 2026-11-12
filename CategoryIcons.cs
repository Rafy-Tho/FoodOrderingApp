namespace FoodOrderingApp;

/// <summary>
/// Maps the human readable icon names used in categories.json to Font Awesome
/// glyphs. Unknown names fall back to the utensils icon.
/// </summary>
public static class CategoryIcons
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["all"] = Icons.Utensils,
        ["utensils"] = Icons.Utensils,
        ["burger"] = Icons.Burger,
        ["burgers"] = Icons.Burger,
        ["pizza"] = Icons.PizzaSlice,
        ["asian"] = Icons.BowlRice,
        ["noodles"] = Icons.BowlRice,
        ["rice"] = Icons.BowlRice,
        ["sushi"] = Icons.Fish,
        ["fish"] = Icons.Fish,
        ["chicken"] = Icons.Drumstick,
        ["drinks"] = Icons.MugHot,
        ["coffee"] = Icons.MugHot,
        ["tea"] = Icons.MugHot,
        ["juice"] = Icons.Lemon,
        ["salad"] = Icons.Leaf,
        ["healthy"] = Icons.Leaf,
        ["vegetarian"] = Icons.Leaf,
        ["spicy"] = Icons.PepperHot,
        ["dessert"] = Icons.IceCream,
        ["desserts"] = Icons.IceCream,
        ["icecream"] = Icons.IceCream,
        ["cake"] = Icons.CakeCandles,
        ["cookies"] = Icons.CookieBite,
    };

    public static string Resolve(string? key) =>
        !string.IsNullOrWhiteSpace(key) && Map.TryGetValue(key, out string? glyph)
            ? glyph
            : Icons.Utensils;
}
