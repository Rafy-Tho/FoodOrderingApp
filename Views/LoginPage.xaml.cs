using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class LoginPage : ContentPage
{
    private bool _isBusy;

    public LoginPage()
    {
        InitializeComponent();
    }

    // ---------- Small helpers ----------

    private void OnEmailCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();

    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e) => HideError();

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordButton.Text = PasswordEntry.IsPassword ? Icons.Eye : Icons.EyeSlash;
    }

    private void OnUseDemoTapped(object? sender, TappedEventArgs e)
    {
        EmailEntry.Text = "demo@foodexpress.com";
        PasswordEntry.Text = "demo123";
        HideError();
    }

    private void OnUseAdminTapped(object? sender, TappedEventArgs e)
    {
        EmailEntry.Text = "admin@foodexpress.com";
        PasswordEntry.Text = "admin123";
        HideError();
    }

    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    // ---------- Sign in ----------

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        if (_isBusy) return;

        HideError();
        _isBusy = true;
        SignInButton.IsEnabled = false;
        SignInButton.Text = "Signing in...";

        try
        {
            string email = EmailEntry.Text ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            // Hashing runs off the UI thread so the button state paints first.
            AuthResult result = await Task.Run(() => AuthService.Login(email, password));

            if (!result.Success)
            {
                ShowError(result.Error ?? "Unable to sign in.");
                return;
            }

            App.ShowHome();
        }
        finally
        {
            _isBusy = false;
            SignInButton.IsEnabled = true;
            SignInButton.Text = "Sign in";
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError() => ErrorBorder.IsVisible = false;
}
