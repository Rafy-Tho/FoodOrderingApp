using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SQLite;

namespace FoodOrderingApp.Models;

public class Order : INotifyPropertyChanged
{
    private string _status = "Pending";

    [PrimaryKey]
    public int Id { get; set; }

    /// <summary>Owner of the order (user id).</summary>
    [Indexed]
    public string UserId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash on Delivery";

    /// <summary>Stored in the OrderItem table, not as a column.</summary>
    [Ignore]
    public List<OrderItem> Items { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            NotifyStatusChanged();
        }
    }

    // ---------- Display helpers for the Order History page ----------
    [JsonIgnore, Ignore] public string OrderIdText => $"Order #{Id}";
    [JsonIgnore, Ignore] public string TotalText => $"${Total:F2}";
    [JsonIgnore, Ignore] public string OrderDateText => OrderDate.ToString("MMM dd, yyyy  hh:mm tt", CultureInfo.InvariantCulture);
    [JsonIgnore, Ignore] public string CustomerInfo => $"{CustomerName} • {PhoneNumber}";
    [JsonIgnore, Ignore] public string ItemsSummary => string.Join("\n", Items.Select(i => $"{i.Quantity} × {i.FoodName}"));

    /// <summary>Only pending orders can be cancelled.</summary>
    [JsonIgnore, Ignore] public bool CanCancel => Status == "Pending";

    // Status badge colors (text color and soft background color)
    [JsonIgnore, Ignore]
    public Color StatusColor => Status switch
    {
        "Delivered" => Color.FromArgb("#2E7D32"),
        "Cancelled" => Color.FromArgb("#C62828"),
        _ => Color.FromArgb("#E65100")            // Pending and anything else
    };

    [JsonIgnore, Ignore]
    public Color StatusBackgroundColor => Status switch
    {
        "Delivered" => Color.FromArgb("#E8F5E9"),
        "Cancelled" => Color.FromArgb("#FFEBEE"),
        _ => Color.FromArgb("#FFF1E0")
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Refreshes the status pill (and the Cancel button) after an update.</summary>
    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged(nameof(StatusBackgroundColor));
        OnPropertyChanged(nameof(CanCancel));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
