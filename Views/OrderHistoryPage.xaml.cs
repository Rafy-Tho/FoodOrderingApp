using FoodOrderingApp.Models;
using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class OrderHistoryPage : ContentPage
{
    public OrderHistoryPage()
    {
        InitializeComponent();

        // Shows the signed-in user's orders from the Order / OrderItem tables
        OrdersCollection.ItemsSource = OrderService.Orders;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Reload from the database every time so edits made outside the app show up too.
        OrderService.LoadForCurrentUser();
        UpdateView();
    }

    private void UpdateView()
    {
        int orderCount = OrderService.Orders.Count;
        bool hasOrders = orderCount > 0;

        // Show either the list or the empty message
        OrdersCollection.IsVisible = hasOrders;
        EmptyState.IsVisible = !hasOrders;

        decimal totalSpent = OrderService.Orders
            .Where(o => o.Status != "Cancelled")
            .Sum(o => o.Total);

        string orderWord = orderCount == 1 ? "order" : "orders";
        OrderSummaryLabel.Text = $"{orderCount} {orderWord}  •  ${totalSpent:F2} spent in total";
        Header.Subtitle = $"{orderCount} {orderWord}  •  ${totalSpent:F2} spent";
    }

    // ---------- Order actions (update / delete in the database) ----------

    private async void OnCancelOrderClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Order order)
            return;

        bool confirmed = await DisplayAlert(
            "Cancel order",
            $"Cancel {order.OrderIdText}? This can't be undone.",
            "Yes, cancel", "Keep it");

        if (!confirmed) return;

        OrderService.UpdateStatus(order, "Cancelled");
        UpdateView();
    }

    private async void OnDeleteOrderClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not Order order)
            return;

        bool confirmed = await DisplayAlert(
            "Delete order",
            $"Remove {order.OrderIdText} from your history?\n\nThis deletes it from the saved file.",
            "Delete", "Keep");

        if (!confirmed) return;

        OrderService.DeleteOrder(order);
        UpdateView();
    }

    private async void OnStartOrderingClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(); // back to the Home Page
    }
}
