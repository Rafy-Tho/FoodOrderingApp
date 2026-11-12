using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class AccountPage : ContentPage
{
    public AccountPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        User? user = AuthService.CurrentUser;

        InitialLabel.Text = user?.Initial ?? "?";
        NameLabel.Text = user?.FullName ?? "Guest";
        EmailLabel.Text = user?.Email ?? string.Empty;

        OrdersCountLabel.Text = OrderService.Orders.Count.ToString();
        CartCountLabel.Text = CartService.GetCartItemCount().ToString();

        AdminCard.IsVisible = user?.IsAdmin == true;
    }

    private async void OnAdminTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AdminPage());
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Sign out",
            "Your cart and order history stay saved for next time.",
            "Sign out", "Cancel");

        if (!confirmed) return;

        AuthService.Logout();
        App.ShowLogin();
    }
}
