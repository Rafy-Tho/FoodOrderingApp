using System.Text.Json.Serialization;
using SQLite;

namespace FoodOrderingApp.Models;

/// <summary>A registered account. Passwords are stored salted + hashed (see AuthService).</summary>
public class User
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string FullName { get; set; } = string.Empty;

    [Indexed(Unique = true)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Admins can open the dashboard (menu, categories, users, orders).</summary>
    public bool IsAdmin { get; set; }

    [JsonIgnore, Ignore] public string RoleText => IsAdmin ? "Admin" : "Customer";

    [JsonIgnore, Ignore] public string JoinedText => $"Joined {CreatedAt:MMM dd, yyyy}";

    [JsonIgnore, Ignore]
    public string FirstName
    {
        get
        {
            string[] parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "there";
        }
    }

    [JsonIgnore, Ignore]
    public string Initial =>
        string.IsNullOrWhiteSpace(FullName) ? "?" : char.ToUpperInvariant(FullName.Trim()[0]).ToString();
}
