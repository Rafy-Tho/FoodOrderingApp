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

    public static void SaveCategories() =>
        Database.SaveCategories(Categories.Where(c => c.Name != AllCategory.Name));

    /// <summary>Adds a filter chip and saves it. Returns null when the name is empty or already used.</summary>
    public static FoodCategory? AddCategory(string name, string iconKey)
    {
        name = (name ?? string.Empty).Trim();

        if (name.Length == 0)
            return null;

        if (Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            return null;

        FoodCategory category = new(name, string.IsNullOrWhiteSpace(iconKey) ? "utensils" : iconKey.Trim());
        Categories.Add(category);
        SaveCategories();

        return category;
    }

    /// <summary>
    /// Renames a category and/or changes its icon. Every dish in the old
    /// category moves to the new name. Returns false when the name is empty,
    /// the category is missing, or the new name is already taken.
    /// </summary>
    public static bool UpdateCategory(string originalName, string newName, string iconKey)
    {
        newName = (newName ?? string.Empty).Trim();

        if (newName.Length == 0)
            return false;

        FoodCategory? category = Categories.FirstOrDefault(c => c.Name == originalName);

        if (category is null)
            return false;

        bool renamed = !string.Equals(originalName, newName, StringComparison.Ordinal);

        if (renamed && Categories.Any(c => string.Equals(c.Name, newName, StringComparison.OrdinalIgnoreCase)))
            return false;

        int index = Categories.IndexOf(category);

        Categories[index] = new FoodCategory(
            newName,
            string.IsNullOrWhiteSpace(iconKey) ? category.IconKey : iconKey.Trim());

        if (renamed)
        {
            foreach (Food food in Foods.Where(f => f.Category == originalName))
                food.Category = newName;

            SaveFoods();
        }

        SaveCategories();
        return true;
    }

    /// <summary>
    /// Deletes a category and every dish that belongs to it. Returns the number
    /// of dishes that were removed, so the caller can clean up carts.
    /// </summary>
    public static int DeleteCategory(string name)
    {
        FoodCategory? category = Categories.FirstOrDefault(c => c.Name == name);

        if (category is null)
            return 0;

        List<Food> items = Foods.Where(f => f.Category == name).ToList();

        foreach (Food food in items)
            Foods.Remove(food);

        Categories.Remove(category);

        if (items.Count > 0)
            SaveFoods();

        SaveCategories();
        return items.Count;
    }

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
        // Match by Id: the admin edits a copy of the row, not the same instance.
        Food? existing = GetById(food.Id);
        if (existing is null) return;

        int index = Foods.IndexOf(existing);
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
