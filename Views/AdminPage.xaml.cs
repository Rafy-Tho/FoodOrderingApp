using System.Collections.ObjectModel;
using System.Globalization;
using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

/// <summary>
/// Store-admin tools: add products, add categories, browse registered users and
/// confirm pending orders. Only reachable when the signed-in account is an admin.
/// </summary>
public partial class AdminPage : ContentPage
{
    private static readonly Color Orange = Color.FromArgb("#FF6B35");
    private static readonly Color Ink = Color.FromArgb("#1E232B");
    private static readonly Color LineColor = Color.FromArgb("#F0E7DF");

    // Bundled photos that ship in Resources\Images.
    private static readonly string[] FoodImages =
    {
        "food_cheeseburger.jpg",
        "food_double_burger.jpg",
        "food_chicken_burger.jpg",
        "food_margherita.jpg",
        "food_pepperoni.jpg",
        "food_veggie_pizza.jpg",
        "food_fried_rice.jpg",
        "food_beef_noodles.jpg",
        "food_salmon_sushi.jpg",
        "food_lemon_tea.jpg",
        "food_orange_juice.jpg",
        "food_milkshake.jpg",
        "food_lava_cake.jpg",
        "food_ice_cream.jpg"
    };

    // Icon names understood by CategoryIcons.
    private static readonly string[] IconKeys =
    {
        "burgers", "pizza", "asian", "sushi", "chicken", "drinks",
        "coffee", "juice", "salad", "spicy", "desserts", "cake",
        "cookies", "utensils"
    };

    private readonly ObservableCollection<FoodCategory> _categories = new();
    private readonly ObservableCollection<User> _users = new();
    private readonly ObservableCollection<Order> _orders = new();

    private Button? _activeTab;

    // Non-null while the category form is editing an existing category.
    private string? _editingCategoryName;
    private string? _editingCategoryIcon;

    // Non-null while the product form is editing an existing product.
    private int? _editingProductId;
    private string? _editingProductImage;

    public AdminPage()
    {
        InitializeComponent();

        // Lists refresh themselves: Foods is an ObservableCollection, the other
        // three are rebuilt by the Reload... methods.
        BindableLayout.SetItemsSource(ProductListLayout, MenuService.Foods);
        BindableLayout.SetItemsSource(CategoryListLayout, _categories);
        BindableLayout.SetItemsSource(UsersListLayout, _users);
        BindableLayout.SetItemsSource(OrdersListLayout, _orders);

        ProductImagePicker.ItemsSource = FoodImages.ToList();
        ProductImagePicker.SelectedIndex = 0;

        CategoryIconPicker.ItemsSource = IconKeys.ToList();
        CategoryIconPicker.SelectedIndex = 0;

        SelectTab(ProductsTabButton);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Guard: never show store tools to a normal customer.
        if (AuthService.CurrentUser?.IsAdmin != true)
        {
            Dispatcher.Dispatch(async () =>
            {
                await DisplayAlert("Admins only",
                    "This area is only available to the store admin.", "OK");
                await Navigation.PopAsync();
            });
            return;
        }

        ReloadCategoryOptions();
        ReloadCategories();
        ReloadUsers();
        ReloadOrders();
        UpdateProductCount();
        SelectTab(_activeTab ?? ProductsTabButton);
    }

    // ---------- Tabs ----------

    private void OnTabClicked(object? sender, EventArgs e)
    {
        if (sender is Button tab)
            SelectTab(tab);
    }

    private void SelectTab(Button tab)
    {
        _activeTab = tab;

        ProductsView.IsVisible = tab == ProductsTabButton;
        CategoriesView.IsVisible = tab == CategoriesTabButton;
        UsersView.IsVisible = tab == UsersTabButton;
        OrdersView.IsVisible = tab == OrdersTabButton;

        foreach (var child in TabsRow.Children)
        {
            if (child is not Button button)
                continue;

            bool selected = button == tab;
            button.BackgroundColor = selected ? Orange : Colors.White;
            button.TextColor = selected ? Colors.White : Ink;
            button.BorderColor = selected ? Orange : LineColor;
        }
    }

    // ---------- Products ----------

    private void UpdateProductCount()
    {
        int count = MenuService.Foods.Count;
        ProductCountLabel.Text = count == 1 ? "1 item" : $"{count} items";
    }

    /// <summary>
    /// Re-binds the product list. Needed after a category rename, because Food
    /// rows show the category name and Food does not raise property changes.
    /// </summary>
    private void RefreshProductList()
    {
        BindableLayout.SetItemsSource(ProductListLayout, null);
        BindableLayout.SetItemsSource(ProductListLayout, MenuService.Foods);
    }

    private async void OnSaveProductClicked(object? sender, EventArgs e)
    {
        ProductErrorLabel.IsVisible = false;

        string name = ProductNameEntry.Text?.Trim() ?? string.Empty;
        string description = ProductDescriptionEntry.Text?.Trim() ?? string.Empty;
        string priceText = (ProductPriceEntry.Text ?? string.Empty).Trim().TrimStart('$');

        if (name.Length < 2)
        {
            ShowProductError("Please enter a product name (at least 2 characters).");
            return;
        }

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price)
            || price <= 0)
        {
            ShowProductError("Please enter a valid price, e.g. 8.99.");
            return;
        }

        if (ProductCategoryPicker.SelectedItem is not string categoryName || categoryName.Length == 0)
        {
            ShowProductError("Pick a category. You can add one on the Categories tab.");
            return;
        }

        string image = ProductImagePicker.SelectedItem as string
                       ?? _editingProductImage
                       ?? FoodImages[0];

        Food food = new()
        {
            Name = name,
            Description = description,
            Price = price,
            Category = categoryName,
            Image = image,
            IsPopular = ProductPopularSwitch.IsToggled
        };

        // ---------- Update the product being edited ----------
        if (_editingProductId is int productId)
        {
            food.Id = productId;

            MenuService.UpdateFood(food);
            ResetProductForm();
            ReloadCategoryOptions();
            UpdateProductCount();
            ReloadCategories();
            RefreshProductList();

            // Cart items hold Food objects, so reload to pick up the new price.
            if (CartService.Items.Any(c => c.Food.Id == productId))
                CartService.LoadForCurrentUser();

            await DisplayAlert("Product updated",
                $"{food.Name} was updated.", "OK");
            return;
        }

        // ---------- Add a new product ----------
        MenuService.AddFood(food);
        ResetProductForm();
        ReloadCategoryOptions();
        UpdateProductCount();
        ReloadCategories();     // the new item changes category counts

        await DisplayAlert("Product added",
            $"{food.Name} is now on the menu at {food.PriceText}.", "OK");
    }

    private async void OnEditProductClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Food food)
            return;

        _editingProductId = food.Id;
        _editingProductImage = food.Image;

        ProductFormTitle.Text = "Edit product";
        ProductSaveButton.Text = "Save changes";
        ProductCancelButton.IsVisible = true;

        ProductNameEntry.Text = food.Name;
        ProductDescriptionEntry.Text = food.Description;
        ProductPriceEntry.Text = food.Price.ToString("0.##", CultureInfo.CurrentCulture);
        ProductPopularSwitch.IsToggled = food.IsPopular;
        ProductErrorLabel.IsVisible = false;

        ReloadCategoryOptions();
        ProductCategoryPicker.SelectedItem = food.Category;

        // Make sure the stored photo can be selected even if it is not bundled.
        List<string> images = FoodImages.ToList();
        if (!images.Contains(food.Image))
            images.Insert(0, food.Image);

        ProductImagePicker.ItemsSource = images;
        ProductImagePicker.SelectedItem = food.Image;

        await ProductsView.ScrollToAsync(ProductFormCard, ScrollToPosition.Start, true);
    }

    private void OnCancelProductEditClicked(object? sender, EventArgs e)
    {
        ResetProductForm();
        ReloadCategoryOptions();
    }

    private void ResetProductForm()
    {
        _editingProductId = null;
        _editingProductImage = null;

        ProductFormTitle.Text = "Add product";
        ProductSaveButton.Text = "Add product";
        ProductCancelButton.IsVisible = false;

        ProductNameEntry.Text = string.Empty;
        ProductDescriptionEntry.Text = string.Empty;
        ProductPriceEntry.Text = string.Empty;
        ProductPopularSwitch.IsToggled = false;

        ProductImagePicker.ItemsSource = FoodImages.ToList();
        ProductImagePicker.SelectedIndex = 0;
        ProductErrorLabel.IsVisible = false;
    }

    private async void OnDeleteProductClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Food food)
            return;

        bool confirmed = await DisplayAlert("Remove product",
            $"Remove {food.Name} from the menu?", "Remove", "Keep");

        if (!confirmed) return;

        MenuService.DeleteFood(food.Id);
        UpdateProductCount();
        ReloadCategories();

        if (_editingProductId == food.Id)
            ResetProductForm();

        // Drop it from the current cart too, so the cart can't keep a deleted dish.
        CartItem? line = CartService.Items.FirstOrDefault(c => c.Food.Id == food.Id);
        if (line is not null)
            CartService.RemoveFromCart(line);
    }

    private void ShowProductError(string message)
    {
        ProductErrorLabel.Text = message;
        ProductErrorLabel.IsVisible = true;
    }

    // ---------- Categories ----------

    private void ReloadCategoryOptions()
    {
        string? previous = ProductCategoryPicker.SelectedItem as string;

        List<string> names = MenuService.Categories
            .Where(c => c.Name != "All")
            .Select(c => c.Name)
            .ToList();

        ProductCategoryPicker.ItemsSource = names;
        ProductCategoryPicker.SelectedItem =
            previous is not null && names.Contains(previous) ? previous : names.FirstOrDefault();
    }

    private void ReloadCategories()
    {
        _categories.Clear();

        foreach (FoodCategory category in Database.GetCategories().OrderBy(c => c.Name))
        {
            _categories.Add(new FoodCategory(category.Name, category.IconKey)
            {
                ItemCount = MenuService.Foods.Count(f => f.Category == category.Name)
            });
        }

        CategoryCountLabel.Text = _categories.Count == 1
            ? "1 category"
            : $"{_categories.Count} categories";
    }

    private async void OnSaveCategoryClicked(object? sender, EventArgs e)
    {
        CategoryErrorLabel.IsVisible = false;

        string name = CategoryNameEntry.Text?.Trim() ?? string.Empty;

        if (name.Length < 2)
        {
            ShowCategoryError("Please enter a category name (at least 2 characters).");
            return;
        }

        string iconKey = CategoryIconPicker.SelectedItem as string
                         ?? _editingCategoryIcon
                         ?? "utensils";

        // ---------- Add a new category ----------
        if (_editingCategoryName is null)
        {
            if (MenuService.Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                ShowCategoryError($"\"{name}\" already exists.");
                return;
            }

            if (MenuService.AddCategory(name, iconKey) is null)
            {
                ShowCategoryError("Could not add that category.");
                return;
            }

            ResetCategoryForm();
            ReloadCategoryOptions();
            ReloadCategories();

            await DisplayAlert("Category added",
                $"{name} is now available as a filter on the home page.", "OK");
            return;
        }

        // ---------- Update the category being edited ----------
        string originalName = _editingCategoryName;
        bool renamed = !string.Equals(originalName, name, StringComparison.Ordinal);

        if (!MenuService.UpdateCategory(originalName, name, iconKey))
        {
            ShowCategoryError($"\"{name}\" is already used by another category.");
            return;
        }

        ResetCategoryForm();
        ReloadCategoryOptions();
        ReloadCategories();
        RefreshProductList();

        await DisplayAlert("Category updated",
            renamed
                ? $"\"{originalName}\" is now \"{name}\"."
                : $"\"{name}\" was updated.",
            "OK");
    }

    private async void OnEditCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not FoodCategory category)
            return;

        _editingCategoryName = category.Name;
        _editingCategoryIcon = category.IconKey;

        CategoryFormTitle.Text = "Edit category";
        CategorySaveButton.Text = "Save changes";
        CategoryCancelButton.IsVisible = true;

        CategoryNameEntry.Text = category.Name;
        CategoryErrorLabel.IsVisible = false;

        // Make sure the stored icon can be selected even if it is a custom key.
        List<string> icons = IconKeys.ToList();
        if (!icons.Contains(category.IconKey))
            icons.Insert(0, category.IconKey);

        CategoryIconPicker.ItemsSource = icons;
        CategoryIconPicker.SelectedItem = category.IconKey;

        await CategoriesView.ScrollToAsync(CategoryFormCard, ScrollToPosition.Start, true);
    }

    private async void OnDeleteCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not FoodCategory category)
            return;

        int productCount = MenuService.Foods.Count(f => f.Category == category.Name);

        string message = productCount == 0
            ? $"Delete \"{category.Name}\"?"
            : $"Delete \"{category.Name}\"?\n\n" +
              $"It has {productCount} product{(productCount == 1 ? "" : "s")}. " +
              "They will also be removed from the menu.";

        bool confirmed = await DisplayAlert("Delete category", message, "Delete", "Cancel");

        if (!confirmed) return;

        // Remember the dishes so they can be dropped from the open cart too.
        List<int> removedFoodIds = MenuService.Foods
            .Where(f => f.Category == category.Name)
            .Select(f => f.Id)
            .ToList();

        MenuService.DeleteCategory(category.Name);

        foreach (int foodId in removedFoodIds)
        {
            CartItem? line = CartService.Items.FirstOrDefault(c => c.Food.Id == foodId);
            if (line is not null)
                CartService.RemoveFromCart(line);
        }

        if (_editingCategoryName == category.Name)
            ResetCategoryForm();

        ReloadCategoryOptions();
        ReloadCategories();
        UpdateProductCount();
        RefreshProductList();
    }

    private void OnCancelCategoryEditClicked(object? sender, EventArgs e) => ResetCategoryForm();

    private void ResetCategoryForm()
    {
        _editingCategoryName = null;
        _editingCategoryIcon = null;

        CategoryFormTitle.Text = "Add category";
        CategorySaveButton.Text = "Add category";
        CategoryCancelButton.IsVisible = false;

        CategoryNameEntry.Text = string.Empty;
        CategoryIconPicker.ItemsSource = IconKeys.ToList();
        CategoryIconPicker.SelectedIndex = 0;
        CategoryErrorLabel.IsVisible = false;
    }

    private void ShowCategoryError(string message)
    {
        CategoryErrorLabel.Text = message;
        CategoryErrorLabel.IsVisible = true;
    }

    // ---------- Users ----------

    private void ReloadUsers()
    {
        _users.Clear();

        List<User> users = Database.GetUsers();

        foreach (User user in users)
            _users.Add(user);

        UsersCountLabel.Text = users.Count.ToString();
        AdminsCountLabel.Text = users.Count(u => u.IsAdmin).ToString();
    }

    // ---------- Orders ----------

    private void ReloadOrders()
    {
        _orders.Clear();

        // Pending orders first, then the newest of the rest.
        List<Order> orders = Database.GetAllOrders()
            .OrderBy(o => o.Status == "Pending" ? 0 : 1)
            .ThenByDescending(o => o.OrderDate)
            .ToList();

        foreach (Order order in orders)
            _orders.Add(order);

        UpdateOrderStats();
    }

    private void UpdateOrderStats()
    {
        OrdersCountLabel.Text = _orders.Count.ToString();
        PendingOrdersLabel.Text = _orders.Count(o => o.CanConfirm).ToString();
        DeliveredRevenueLabel.Text =
            $"${_orders.Where(o => o.Status == "Delivered").Sum(o => o.Total):F2}";
    }

    private async void OnConfirmOrderClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Order order)
            return;

        bool confirmed = await DisplayAlert("Confirm order",
            $"Mark {order.OrderIdText} as delivered?\n\n{order.CustomerName}  •  {order.TotalText}",
            "Confirm", "Not yet");

        if (!confirmed) return;

        OrderService.UpdateStatus(order, "Delivered");
        UpdateOrderStats();
    }
}
