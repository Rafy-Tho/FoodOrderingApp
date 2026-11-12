using FoodOrderingApp.Services;

namespace FoodOrderingApp.Views;

public partial class RegisterPage : ContentPage
{
    private bool _isBusy;

    public RegisterPage()
    {
        InitializeComponent();
    }

    // ---------- Small helpers ----------

    private void OnNameCompleted(object? sender, EventArgs e) => EmailEntry.Focus();

    private void OnEmailCompleted(object? sender, EventArgs e) => PasswordEntry.Focus();

    private void OnPasswordCompleted(object? sender, EventArgs e) => ConfirmEntry.Focus();

    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e) => HideError();

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        bool show = PasswordEntry.IsPassword;   // currently hidden -> show both

        PasswordEntry.IsPassword = !show;
        ConfirmEntry.IsPassword = !show;
        TogglePasswordButton.Text = show ? Icons.EyeSlash : Icons.Eye;
    }

    private async void OnSignInTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    // ---------- Create the account ----------

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        if (_isBusy) return;

        HideError();
        _isBusy = true;
        CreateAccountButton.IsEnabled = false;
        CreateAccountButton.Text = "Creating account...";

        try
        {
            string name = NameEntry.Text ?? string.Empty;
            string email = EmailEntry.Text ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;
            string confirm = ConfirmEntry.Text ?? string.Empty;

            // Hashing runs off the UI thread so the button state paints first.
            AuthResult result = await Task.Run(() =>
                AuthService.Register(name, email, password, confirm));

            if (!result.Success)
            {
                ShowError(result.Error ?? "Unable to create the account.");
                return;
            }

            // Register() signs the new user in, so go straight to the app.
            App.ShowHome();
        }
        finally
        {
            _isBusy = false;
            CreateAccountButton.IsEnabled = true;
            CreateAccountButton.Text = "Create account";
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError() => ErrorBorder.IsVisible = false;
}
