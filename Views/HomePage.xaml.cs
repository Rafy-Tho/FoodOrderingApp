using System.Collections.ObjectModel;
using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class HomePage : ContentPage
{
    // The list currently shown in "All food". BindableLayout watches this
    // collection, so refreshing it never touches the search box.
    private readonly ObservableCollection<Food> _visibleFoods = new();

    private static readonly Color Orange = Color.FromArgb("#FF6B35");
    private static readonly Color Ink = Color.FromArgb("#1E232B");
    private static readonly Color LineColor = Color.FromArgb("#F0E7DF");

    private FoodCategory? _selectedCategory;
    private CancellationTokenSource? _searchDebounce;
    private int _toastVersion;

    public HomePage()
    {
        InitializeComponent();

        // Everything is bound once here; only the contents of _visibleFoods change.
        BindableLayout.SetItemsSource(FoodListLayout, _visibleFoods);
        BindableLayout.SetItemsSource(CategoryChips, MenuService.Categories);
        BindableLayout.SetItemsSource(PopularList, MenuService.GetPopularFoods());

        _selectedCategory = MenuService.Categories[0];   // "All"
        UpdateChipVisuals();
        UpdateGreeting();
        RefreshFoods();
    }

    // Runs every time the page is shown, including when we come back from another page
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateCartBar();
        UpdateGreeting();
        UpdateChipVisuals();
    }

    private void UpdateGreeting()
    {
        User? user = AuthService.CurrentUser;

        Header.ProfileInitial = user?.Initial ?? "?";
        Header.Subtitle = user is null
            ? "Deliver to · Home"
            : $"Hi {user.FirstName} · Deliver to Home";
    }

    // ---------- Search (debounced so typing is never interrupted) ----------

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Cancel();

        var cts = new CancellationTokenSource();
        _searchDebounce = cts;

        try
        {
            // Wait until the user pauses typing before filtering.
            await Task.Delay(300, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
            return;

        RefreshFoods();
    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        _searchDebounce?.Cancel();
        RefreshFoods();
        SearchEntry.Unfocus();
    }

    // ---------- Category chips ----------

    private void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        FoodCategory? category = e.Parameter as FoodCategory
                                 ?? (sender as Element)?.BindingContext as FoodCategory;

        if (category is null || category == _selectedCategory)
            return;

        _selectedCategory = category;
        UpdateChipVisuals();
        RefreshFoods();
    }

    private void UpdateChipVisuals()
    {
        foreach (var child in CategoryChips.Children)
        {
            if (child is not Border chip)
                continue;

            bool selected = chip.BindingContext is FoodCategory category
                            && category == _selectedCategory;

            chip.BackgroundColor = selected ? Orange : Colors.White;
            chip.Stroke = new SolidColorBrush(selected ? Orange : LineColor);

            if (chip.Content is not HorizontalStackLayout row)
                continue;

            if (row.Children.Count > 0 && row.Children[0] is Label icon)
                icon.TextColor = selected ? Colors.White : Orange;

            if (row.Children.Count > 1 && row.Children[1] is Label label)
                label.TextColor = selected ? Colors.White : Ink;
        }
    }

    // ---------- Filtering ----------

    private void RefreshFoods()
    {
        string searchText = SearchEntry.Text ?? string.Empty;
        string categoryName = _selectedCategory?.Name ?? "All";
        List<Food> foods = MenuService.SearchFoods(searchText, categoryName);

        _visibleFoods.Clear();
        foreach (Food food in foods)
            _visibleFoods.Add(food);

        AllFoodTitleLabel.Text = categoryName == "All" ? "All food" : categoryName;
        AllFoodCountLabel.Text = foods.Count == 1 ? "1 item" : $"{foods.Count} items";
        NoResultsLabel.IsVisible = foods.Count == 0;

        // Hide "Popular" while the user is searching or filtering
        bool isFiltering = !string.IsNullOrWhiteSpace(searchText) || categoryName != "All";
        PopularSection.IsVisible = !isFiltering;
    }

    // ---------- Cart ----------

    // Shared by the + buttons in BOTH the popular list and the all-food list
    private async void OnAddToCartClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Food food)
            return;

        CartService.AddToCart(food);
        UpdateCartBar();
        await ShowToastAsync($"{food.Name} added to cart");
    }

    private void UpdateCartBar()
    {
        int count = CartService.GetCartItemCount();

        CartBar.IsVisible = count > 0;
        CartCountLabel.Text = count == 1 ? "1 item" : $"{count} items";
        CartTotalLabel.Text = $"${CartService.GetSubtotal():F2}";
        Header.BadgeText = count > 0 ? count.ToString() : string.Empty;
    }

    private async void OnCartBarTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new CartPage());
    }

    // ---------- Navigation ----------

    private async void OnCartClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new CartPage());
    }

    private async void OnOrdersClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new OrderHistoryPage());
    }

    private async void OnProfileClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AccountPage());
    }

    // ---------- Toast message ----------

    private async Task ShowToastAsync(string message)
    {
        // The version number stops an older toast from hiding a newer one
        int version = ++_toastVersion;

        ToastLabel.Text = message;
        ToastBorder.IsVisible = true;
        await ToastBorder.FadeTo(1, 150);

        await Task.Delay(1500);
        if (version != _toastVersion) return;

        await ToastBorder.FadeTo(0, 250);
        if (version == _toastVersion)
            ToastBorder.IsVisible = false;
    }
}
