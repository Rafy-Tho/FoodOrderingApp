using System.Security.Cryptography;
using System.Text;
using FoodOrderingApp.Models;

namespace FoodOrderingApp.Services;

public sealed record AuthResult(bool Success, string? Error = null);

/// <summary>
/// Fake (but real enough) authentication backed by the User table.
/// Passwords are salted and hashed with PBKDF2-SHA256.
/// </summary>
public static class AuthService
{
    private const int Iterations = 100_000;
    private const int HashSize = 32;
    private const string DatabaseSessionKey = "CurrentUserId";

    public static User? CurrentUser { get; private set; }
    public static bool IsLoggedIn => CurrentUser is not null;

    // ---------- Startup ----------

    public static void Initialize()
    {
        // Always make sure there is a demo account to sign in with.
        if (Database.UserCount() == 0)
            Database.InsertUser(CreateUser("Demo User", "demo@foodexpress.com", "demo123"));

        string? sessionId = Database.GetSetting(DatabaseSessionKey);
        CurrentUser = string.IsNullOrWhiteSpace(sessionId)
            ? null
            : Database.FindUser(sessionId);
    }

    // ---------- Register / login / logout ----------

    public static AuthResult Register(string fullName, string email, string password, string confirmPassword)
    {
        fullName = (fullName ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();

        if (fullName.Length < 2)
            return new AuthResult(false, "Please enter your full name.");

        if (!IsValidEmail(email))
            return new AuthResult(false, "Please enter a valid email address.");

        if (Database.FindUserByEmail(email) is not null)
            return new AuthResult(false, "An account with this email already exists. Try signing in.");

        if (password.Length < 6)
            return new AuthResult(false, "Password must be at least 6 characters.");

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            return new AuthResult(false, "Passwords do not match.");

        User user = CreateUser(fullName, email, password);
        Database.InsertUser(user);
        StartSession(user);

        return new AuthResult(true);
    }

    public static AuthResult Login(string email, string password)
    {
        email = (email ?? string.Empty).Trim();

        if (!IsValidEmail(email))
            return new AuthResult(false, "Please enter a valid email address.");

        if (string.IsNullOrEmpty(password))
            return new AuthResult(false, "Please enter your password.");

        User? user = Database.FindUserByEmail(email);

        if (user is null)
            return new AuthResult(false, "No account found for that email. Create one first.");

        if (!VerifyPassword(password, user))
            return new AuthResult(false, "Incorrect password. Please try again.");

        StartSession(user);
        return new AuthResult(true);
    }

    public static void Logout()
    {
        CurrentUser = null;
        Database.DeleteSetting(DatabaseSessionKey);
    }

    public static int AccountCount => Database.UserCount();

    // ---------- Helpers ----------

    private static void StartSession(User user)
    {
        CurrentUser = user;
        Database.SetSetting(DatabaseSessionKey, user.Id);
    }

    private static User CreateUser(string fullName, string email, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        return new User
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = fullName,
            Email = email,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = HashPassword(password, salt)
        };
    }

    private static string HashPassword(string password, byte[] salt)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, User user)
    {
        try
        {
            byte[] salt = Convert.FromBase64String(user.PasswordSalt);
            byte[] expected = Convert.FromBase64String(user.PasswordHash);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        email = email.Trim();
        if (email.Length > 120 || email.Contains(' ')) return false;

        int at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;

        int dot = email.IndexOf('.', at);
        return dot > at + 1 && dot < email.Length - 1;
    }
}
