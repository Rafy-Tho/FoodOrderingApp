using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class CartPage : ContentPage
{
    public CartPage()
    {
        InitializeComponent();

        // The cart list shows the signed-in user's cart (persisted to CartLine)
        CartCollection.ItemsSource = CartService.Items;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateView();
    }

    // ---------- Refresh everything that depends on the cart ----------

    private void UpdateView()
    {
        bool hasItems = CartService.Items.Count > 0;

        // Show either the cart or the empty message
        CartCollection.IsVisible = hasItems;
        EmptyState.IsVisible = !hasItems;
        SummaryPanel.IsVisible = hasItems;

        int count = CartService.GetCartItemCount();
        ItemCountLabel.Text = count == 1 ? "1 item in your cart" : $"{count} items in your cart";
        Header.Subtitle = count == 1 ? "1 item ready to checkout" : $"{count} items ready to checkout";

        SubtotalLabel.Text = $"${CartService.GetSubtotal():F2}";
        DeliveryFeeLabel.Text = $"${CartService.GetDeliveryFee():F2}";
        TotalLabel.Text = $"${CartService.GetTotal():F2}";
    }

    // ---------- Quantity and remove ----------

    private void OnIncreaseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not CartItem item)
            return;

        CartService.IncreaseQuantity(item);
        UpdateView();
    }

    private async void OnDecreaseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not CartItem item)
            return;

        // At quantity 1, "−" means "remove", so confirm first
        if (item.Quantity <= 1)
        {
            await ConfirmAndRemoveAsync(item);
            return;
        }

        CartService.DecreaseQuantity(item);
        UpdateView();
    }

    private async void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not CartItem item)
            return;

        await ConfirmAndRemoveAsync(item);
    }

    private async Task ConfirmAndRemoveAsync(CartItem item)
    {
        bool confirmed = await DisplayAlert(
            "Remove item",
            $"Remove {item.Name} from your cart?",
            "Remove", "Keep");

        if (!confirmed) return;

        CartService.RemoveFromCart(item);
        UpdateView();
    }

    // ---------- Toolbar and buttons ----------

    private async void OnClearCartClicked(object? sender, EventArgs e)
    {
        if (CartService.Items.Count == 0)
        {
            await DisplayAlert("Cart is empty", "There is nothing to clear.", "OK");
            return;
        }

        bool confirmed = await DisplayAlert(
            "Clear cart",
            "Remove all items from your cart?",
            "Clear", "Cancel");

        if (!confirmed) return;

        CartService.ClearCart();
        UpdateView();
    }

    private async void OnCheckoutClicked(object? sender, EventArgs e)
    {
        // Safety check: never continue with an empty cart
        if (CartService.Items.Count == 0)
        {
            await DisplayAlert("Cart is empty",
                "Please add some food before checking out.", "OK");
            UpdateView();
            return;
        }

        await Navigation.PushAsync(new CheckoutPage());
    }

    private async void OnBrowseMenuClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(); // back to the Home Page
    }
}