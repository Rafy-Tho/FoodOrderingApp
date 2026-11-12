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
        DataPathLabel.Text = Database.DatabasePath;
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
