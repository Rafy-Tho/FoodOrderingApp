using Microsoft.Maui.Controls.Shapes;

namespace FoodOrderingApp.Controls;

/// <summary>
/// Shared top bar used by every page: optional back button (or app logo),
/// a title/subtitle and up to two round icon actions.
/// </summary>
public class PageHeader : ContentView
{
    private static readonly Color Ink = Color.FromArgb("#1E232B");
    private static readonly Color Muted = Color.FromArgb("#7A8496");
    private static readonly Color Orange = Color.FromArgb("#FF6B35");

    private readonly Image _logo;
    private readonly Border _backChip;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Border _actionChip;
    private readonly Border _secondaryChip;
    private readonly Label _actionIcon;
    private readonly Label _secondaryIcon;
    private readonly Border _profileChip;
    private readonly Label _profileLabel;
    private readonly Border _badge;
    private readonly Label _badgeLabel;
    private readonly Grid _actionHost;
    private readonly HorizontalStackLayout _actions;

    private bool _showBack = true;
    private bool _showLogo;

    public PageHeader()
    {
        _logo = new Image
        {
            Source = "logo.png",
            WidthRequest = 42,
            HeightRequest = 42,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false
        };

        _backChip = CreateChip(Icons.ArrowLeft, out var backIcon);
        backIcon.FontSize = 17;
        backIcon.TextColor = Ink;
        var backTap = new TapGestureRecognizer();
        backTap.Tapped += OnBackTapped;
        _backChip.GestureRecognizers.Add(backTap);

        _title = new Label
        {
            FontFamily = "OpenSansSemibold",
            FontSize = 19,
            TextColor = Ink,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _subtitle = new Label
        {
            FontSize = 11.5,
            TextColor = Muted,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            IsVisible = false
        };

        var titles = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children = { _title, _subtitle }
        };

        // Action chips (cart, orders, clear cart...)
        _actionChip = CreateChip(Icons.Cart, out _actionIcon);
        _actionIcon.TextColor = Orange;
        var actionTap = new TapGestureRecognizer();
        actionTap.Tapped += (_, _) => ActionClicked?.Invoke(this, EventArgs.Empty);
        _actionChip.GestureRecognizers.Add(actionTap);

        _secondaryChip = CreateChip(Icons.Receipt, out _secondaryIcon);
        _secondaryIcon.TextColor = Ink;
        _secondaryChip.IsVisible = false;
        var secondaryTap = new TapGestureRecognizer();
        secondaryTap.Tapped += (_, _) => SecondaryActionClicked?.Invoke(this, EventArgs.Empty);
        _secondaryChip.GestureRecognizers.Add(secondaryTap);

        // Round avatar that opens the account page
        _profileLabel = new Label
        {
            FontFamily = "OpenSansSemibold",
            FontSize = 15,
            TextColor = Orange,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _profileChip = new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            BackgroundColor = Color.FromArgb("#FFF1E8"),
            Stroke = Orange,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 21 },
            Content = _profileLabel,
            IsVisible = false,
            VerticalOptions = LayoutOptions.Center
        };

        var profileTap = new TapGestureRecognizer();
        profileTap.Tapped += (_, _) => ProfileClicked?.Invoke(this, EventArgs.Empty);
        _profileChip.GestureRecognizers.Add(profileTap);

        _badgeLabel = new Label
        {
            FontSize = 9,
            FontFamily = "OpenSansSemibold",
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _badge = new Border
        {
            BackgroundColor = Orange,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            WidthRequest = 18,
            HeightRequest = 18,
            IsVisible = false,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, -2, -4, 0),
            Content = _badgeLabel
        };

        _actionHost = new Grid
        {
            IsVisible = false,
            VerticalOptions = LayoutOptions.Center,
            Children = { _actionChip, _badge }
        };

        _actions = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children = { _profileChip, _secondaryChip, _actionHost }
        };

        var root = new Grid
        {
            ColumnSpacing = 12,
            Padding = new Thickness(16, 10, 16, 4),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                new Grid { Children = { _logo, _backChip }, VerticalOptions = LayoutOptions.Center },
                titles,
                _actions
            }
        };

        Grid.SetColumn(titles, 1);
        Grid.SetColumn(_actions, 2);

        Content = root;
        UpdateLeftSlot();
    }

    // ---------- Public surface ----------

    public string Title
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value ?? string.Empty;
    }

    public string Subtitle
    {
        get => _subtitle.Text ?? string.Empty;
        set
        {
            _subtitle.Text = value ?? string.Empty;
            _subtitle.IsVisible = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool ShowBack
    {
        get => _showBack;
        set { _showBack = value; UpdateLeftSlot(); }
    }

    public bool ShowLogo
    {
        get => _showLogo;
        set { _showLogo = value; UpdateLeftSlot(); }
    }

    public bool ShowAction
    {
        get => _actionHost.IsVisible;
        set => _actionHost.IsVisible = value;
    }

    public string ActionIcon
    {
        get => _actionIcon.Text ?? string.Empty;
        set => _actionIcon.Text = value ?? string.Empty;
    }

    public string SecondaryActionIcon
    {
        get => _secondaryIcon.Text ?? string.Empty;
        set
        {
            _secondaryIcon.Text = value ?? string.Empty;
            _secondaryChip.IsVisible = !string.IsNullOrWhiteSpace(value);
        }
    }

    public string BadgeText
    {
        set
        {
            _badgeLabel.Text = value ?? string.Empty;
            _badge.IsVisible = !string.IsNullOrWhiteSpace(value);
        }
    }

    /// <summary>Shows the round avatar chip (opens the account page).</summary>
    public bool ShowProfile
    {
        get => _profileChip.IsVisible;
        set => _profileChip.IsVisible = value;
    }

    /// <summary>One or two letters shown inside the avatar chip.</summary>
    public string ProfileInitial
    {
        set => _profileLabel.Text = value ?? string.Empty;
    }

    public event EventHandler? ActionClicked;
    public event EventHandler? SecondaryActionClicked;
    public event EventHandler? ProfileClicked;

    // ---------- Helpers ----------

    private void UpdateLeftSlot()
    {
        _backChip.IsVisible = _showBack;
        _logo.IsVisible = _showLogo && !_showBack;
    }

    private static Border CreateChip(string glyph, out Label icon)
    {
        icon = new Label
        {
            Text = glyph,
            FontFamily = "FASolid",
            FontSize = 15,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        return new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#F0E7DF"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = icon,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        Element? element = this;
        while (element is not null && element is not Page)
            element = element.Parent;

        if (element is Page page && page.Navigation.NavigationStack.Count > 1)
            await page.Navigation.PopAsync();
    }
}
