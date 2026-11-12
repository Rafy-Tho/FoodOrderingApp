using System.Collections.ObjectModel;
using FoodOrderingApp.Models;

namespace FoodOrderingApp.Services;

/// <summary>
/// Menu data loaded from the Food / FoodCategory tables. The home page binds
/// straight to these collections; editing the seed JSON and rebuilding the app
/// re-imports the menu.
/// </summary>
public static class MenuService
{
    public static ObservableCollection<Food> Foods { get; } = new();

    /// <summary>Includes the synthetic "All" chip at index 0.</summary>
    public static ObservableCollection<FoodCategory> Categories { get; } = new();

    private static readonly FoodCategory AllCategory = new("All", "all");

    public static void Initialize()
    {
        Foods.Clear();
        Categories.Clear();

        foreach (Food food in Database.GetFoods())
            Foods.Add(food);

        Categories.Add(AllCategory);
        foreach (FoodCategory category in Database.GetCategories())
            Categories.Add(category);
    }

    public static void Reload() => Initialize();

    // ---------- Queries ----------

    public static Food? GetById(int id) => Foods.FirstOrDefault(f => f.Id == id);

    public static List<Food> GetPopularFoods() => Foods.Where(f => f.IsPopular).ToList();

    // Filters by category and/or name. Pass "All" or null for no category filter.
    public static List<Food> SearchFoods(string? searchText, string? category)
    {
        IEnumerable<Food> result = Foods;

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            result = result.Where(f => f.Category == category);

        if (!string.IsNullOrWhiteSpace(searchText))
            result = result.Where(f =>
                f.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase));

        return result.ToList();
    }

    // ---------- CRUD (persists the Food table) ----------

    public static void SaveFoods() => Database.SaveFoods(Foods);

    public static Food AddFood(Food food)
    {
        if (food.Id <= 0)
            food.Id = Foods.Count == 0 ? 1 : Foods.Max(f => f.Id) + 1;

        Foods.Add(food);
        SaveFoods();
        return food;
    }

    public static void UpdateFood(Food food)
    {
        int index = Foods.IndexOf(food);
        if (index < 0) return;

        Foods[index] = food;
        SaveFoods();
    }

    public static bool DeleteFood(int id)
    {
        Food? food = GetById(id);
        if (food is null) return false;

        Foods.Remove(food);
        SaveFoods();
        return true;
    }
}
