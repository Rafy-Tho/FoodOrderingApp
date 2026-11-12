using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class CheckoutPage : ContentPage
{
    private const string PaymentMethod = "Cash on Delivery";

    // Stops a double-tap from placing the order twice
    private bool _isPlacingOrder;

    public CheckoutPage()
    {
        InitializeComponent();

        // Fill the order summary list from the signed-in user's cart
        BindableLayout.SetItemsSource(SummaryItemsLayout, CartService.Items);
        RefreshTotals();

        Header.Subtitle = $"{CartService.GetCartItemCount()} items  •  Cash on delivery";

        // Prefill the name from the account
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
            NameEntry.Text = AuthService.CurrentUser?.FullName ?? string.Empty;

        // Android only: resize the page when the keyboard opens
        if (Application.Current != null)
        {
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.UseWindowSoftInputModeAdjust(
                Application.Current.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>(),
                Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Safety check: checkout makes no sense with an empty cart
        if (CartService.Items.Count == 0)
        {
            await DisplayAlert("Cart is empty",
                "Please add some food before checking out.", "OK");
            await Navigation.PopAsync();
            return;
        }

        RefreshTotals();
    }

    // ---------- Totals ----------

    private void RefreshTotals()
    {
        SubtotalLabel.Text = $"${CartService.GetSubtotal():F2}";
        DeliveryFeeLabel.Text = $"${CartService.GetDeliveryFee():F2}";
        TotalLabel.Text = $"${CartService.GetTotal():F2}";
        PlaceOrderButton.Text = $"Place Order  •  ${CartService.GetTotal():F2}";
    }

    // ---------- Keyboard "Next" key moves to the next field ----------

    private void OnNameCompleted(object? sender, EventArgs e) => PhoneEntry.Focus();

    private void OnPhoneCompleted(object? sender, EventArgs e) => AddressEditor.Focus();

    // Hide a field's error message as soon as the user starts typing again
    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender == NameEntry) NameErrorLabel.IsVisible = false;
        else if (sender == PhoneEntry) PhoneErrorLabel.IsVisible = false;
        else if (sender == AddressEditor) AddressErrorLabel.IsVisible = false;
    }

    // ---------- Validation ----------

    // Returns the first invalid control (so we can scroll to it), or null if everything is valid
    private VisualElement? ValidateForm()
    {
        VisualElement? firstInvalid = null;

        string name = NameEntry.Text?.Trim() ?? string.Empty;
        string phone = PhoneEntry.Text?.Trim() ?? string.Empty;
        string address = AddressEditor.Text?.Trim() ?? string.Empty;

        // Name: at least 2 characters
        if (name.Length < 2)
        {
            ShowError(NameErrorLabel, "Please enter your full name (at least 2 characters).");
            firstInvalid ??= NameEntry;
        }

        // Phone: only digits and + - ( ) space, with 8 to 15 digits
        string digits = new string(phone.Where(char.IsDigit).ToArray());
        bool hasOnlyAllowedCharacters = phone.All(c => char.IsDigit(c) || " +-()".Contains(c));

        if (phone.Length == 0)
        {
            ShowError(PhoneErrorLabel, "Please enter your phone number.");
            firstInvalid ??= PhoneEntry;
        }
        else if (!hasOnlyAllowedCharacters || digits.Length < 8 || digits.Length > 15)
        {
            ShowError(PhoneErrorLabel, "Enter a valid phone number (8 to 15 digits).");
            firstInvalid ??= PhoneEntry;
        }

        // Address: at least 10 characters so it is usable for delivery
        if (address.Length < 10)
        {
            ShowError(AddressErrorLabel, "Please enter a full delivery address (at least 10 characters).");
            firstInvalid ??= AddressEditor;
        }

        return firstInvalid;
    }

    private static void ShowError(Label label, string message)
    {
        label.Text = message;
        label.IsVisible = true;
    }

    // ---------- Place order ----------

    private async void OnPlaceOrderClicked(object? sender, EventArgs e)
    {
        if (_isPlacingOrder) return;

        // 1. Cart must not be empty
        if (CartService.Items.Count == 0)
        {
            await DisplayAlert("Cart is empty",
                "Please add some food before placing an order.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // 2. Validate the form; if something is wrong, scroll to it and focus it
        VisualElement? invalidField = ValidateForm();
        if (invalidField != null)
        {
            await MainScrollView.ScrollToAsync(invalidField, ScrollToPosition.Center, true);
            invalidField.Focus();
            return;
        }

        // 3. Place the order
        _isPlacingOrder = true;
        PlaceOrderButton.IsEnabled = false;
        PlaceOrderButton.Text = "Placing order...";

        try
        {
            Order? order = OrderService.PlaceOrder(
                NameEntry.Text ?? string.Empty,
                PhoneEntry.Text ?? string.Empty,
                AddressEditor.Text ?? string.Empty,
                PaymentMethod);

            if (order == null)
            {
                await DisplayAlert("Something went wrong",
                    "We couldn't place your order because your cart is empty.", "OK");
                await Navigation.PopAsync();
                return;
            }

            await DisplayAlert("🎉 Order placed!",
                $"Thank you, {order.CustomerName}!\n\n" +
                $"{order.OrderIdText}\n" +
                $"Total: {order.TotalText} ({order.PaymentMethod})\n" +
                $"Delivering to: {order.DeliveryAddress}\n\n" +
                $"Status: {order.Status}",
                "Great!");

            // Back to the Home Page (the cart is empty, so the cart bar is hidden there)
            await Navigation.PopToRootAsync();
        }
        finally
        {
            _isPlacingOrder = false;
            PlaceOrderButton.IsEnabled = true;
            RefreshTotals();
        }
    }
}