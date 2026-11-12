using FoodOrderingApp.Models;

namespace FoodOrderingApp.Services;

/// <summary>
/// Hard-coded fallback menu, used only when the seed JSON packed into the app
/// (Data\FoodExpressDb\foods.json + categories.json) cannot be read.
/// The JSON files are the source of truth: add, edit or delete dishes there,
/// rebuild, and the menu changes.
/// </summary>
public static class SeedData
{
    public static List<Food> Foods() => new()
    {
        new() { Id = 1, Name = "Classic Cheeseburger", Description = "Juicy beef patty, cheddar, lettuce and tomato", Price = 5.99m, Category = "Burgers", Image = "food_cheeseburger.jpg", IsPopular = true },
        new() { Id = 2, Name = "Double Beef Burger", Description = "Two beef patties with special house sauce", Price = 7.99m, Category = "Burgers", Image = "food_double_burger.jpg" },
        new() { Id = 3, Name = "Crispy Chicken Burger", Description = "Golden fried chicken with creamy mayo", Price = 6.49m, Category = "Burgers", Image = "food_chicken_burger.jpg" },

        new() { Id = 4, Name = "Margherita Pizza", Description = "Tomato sauce, fresh mozzarella and basil", Price = 8.99m, Category = "Pizza", Image = "food_margherita.jpg", IsPopular = true },
        new() { Id = 5, Name = "Pepperoni Pizza", Description = "Loaded with spicy pepperoni and cheese", Price = 9.99m, Category = "Pizza", Image = "food_pepperoni.jpg", IsPopular = true },
        new() { Id = 6, Name = "Veggie Supreme Pizza", Description = "Mushrooms, peppers, olives and onions", Price = 9.49m, Category = "Pizza", Image = "food_veggie_pizza.jpg" },

        new() { Id = 7, Name = "Chicken Fried Rice", Description = "Wok-fried rice with chicken and vegetables", Price = 5.49m, Category = "Asian", Image = "food_fried_rice.jpg" },
        new() { Id = 8, Name = "Beef Noodle Stir Fry", Description = "Savory noodles with tender beef strips", Price = 6.49m, Category = "Asian", Image = "food_beef_noodles.jpg", IsPopular = true },
        new() { Id = 9, Name = "Salmon Sushi Set", Description = "8 pieces of fresh salmon nigiri", Price = 11.99m, Category = "Asian", Image = "food_salmon_sushi.jpg" },

        new() { Id = 10, Name = "Iced Lemon Tea", Description = "Refreshing black tea with fresh lemon", Price = 2.49m, Category = "Drinks", Image = "food_lemon_tea.jpg" },
        new() { Id = 11, Name = "Fresh Orange Juice", Description = "100% squeezed oranges, no sugar added", Price = 2.99m, Category = "Drinks", Image = "food_orange_juice.jpg" },
        new() { Id = 12, Name = "Chocolate Milkshake", Description = "Thick, creamy and topped with whipped cream", Price = 3.99m, Category = "Drinks", Image = "food_milkshake.jpg", IsPopular = true },

        new() { Id = 13, Name = "Chocolate Lava Cake", Description = "Warm cake with a molten chocolate center", Price = 4.99m, Category = "Desserts", Image = "food_lava_cake.jpg", IsPopular = true },
        new() { Id = 14, Name = "Vanilla Ice Cream", Description = "Three scoops of creamy vanilla", Price = 3.49m, Category = "Desserts", Image = "food_ice_cream.jpg" },
    };

    public static List<FoodCategory> Categories() => new()
    {
        new("Burgers", "burgers"),
        new("Pizza", "pizza"),
        new("Asian", "asian"),
        new("Drinks", "drinks"),
        new("Desserts", "desserts"),
    };
}
