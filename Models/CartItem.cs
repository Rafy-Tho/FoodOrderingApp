using System.ComponentModel;

namespace FoodOrderingApp.Models;

public class CartItem : INotifyPropertyChanged
{
    private int _quantity;

    public CartItem(Food food, int quantity = 1)
    {
        Food = food;
        _quantity = quantity;
    }

    public Food Food { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value) return;
            _quantity = value;
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalPriceText));
        }
    }

    // Convenience properties for XAML bindings
    public string Name => Food.Name;
    public string Image => Food.Image;
    public string UnitPriceText => Food.PriceText;

    public decimal TotalPrice => Food.Price * Quantity;
    public string TotalPriceText => $"${TotalPrice:F2}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
