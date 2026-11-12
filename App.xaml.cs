using FoodOrderingApp.Services;

namespace FoodOrderingApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;

        // Set up the database and load the menu / accounts before any page appears.
        Database.Initialize();
        MenuService.Initialize();
        AuthService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Restore the signed-in user's cart and orders from the database.
        if (AuthService.IsLoggedIn)
        {
            CartService.LoadForCurrentUser();
            OrderService.LoadForCurrentUser();
        }

        Page root = AuthService.IsLoggedIn
            ? new Views.HomePage()
            : new Views.LoginPage();

        var window = new Window(BuildNavigation(root))
        {
            Title = "FoodExpress"
        };

#if WINDOWS
        // Only compiled for the Windows build; Android ignores this block
        window.Width = 480;
        window.Height = 860;
#endif

        return window;
    }

    /// <summary>Swaps the window over to the signed-in app (after login or register).</summary>
    public static void ShowHome()
    {
        CartService.LoadForCurrentUser();
        OrderService.LoadForCurrentUser();
        SetRoot(new Views.HomePage());
    }

    /// <summary>Swaps the window back to the login page (after signing out).</summary>
    public static void ShowLogin()
    {
        CartService.ResetForLogout();
        OrderService.ResetForLogout();
        SetRoot(new Views.LoginPage());
    }

    private static NavigationPage BuildNavigation(Page root) => new(root)
    {
        BarBackgroundColor = Color.FromArgb("#FF6B35"),
        BarTextColor = Colors.White
    };

    private static void SetRoot(Page page)
    {
        if (Application.Current?.Windows.FirstOrDefault() is not Window window)
            return;

        window.Page = BuildNavigation(page);
    }
}
