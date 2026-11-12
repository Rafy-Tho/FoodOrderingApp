using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodOrderingApp.Models;
using SQLite;

namespace FoodOrderingApp.Services;

/// <summary>
/// SQLite store for the whole app. One file replaces the old folder of JSON
/// files:
///
///   Windows : &lt;project&gt;\Data\FoodExpressDb\FoodExpress.db3
///   Android : /data/user/0/&lt;package&gt;/files/FoodExpressDb/FoodExpress.db3
///
/// Tables: User, Food, FoodCategory, CartLine, Order, OrderItem, Setting.
///
/// On first launch the old JSON files (if any) are imported once, then the menu
/// is seeded from the foods.json / categories.json packed into the app.
/// </summary>
public static class Database
{
    private const string SeedHashKey = "SeedHash";
    private const string SessionKey = "CurrentUserId";

    private static readonly object Gate = new();
    private static SQLiteConnection? _connection;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // Previous location (also the migration source).
    private static readonly string LegacyRoot = Path.Combine(FileSystem.AppDataDirectory, "FoodExpressDb");

    public static string Root { get; } = ResolveRoot();

    public static string DatabasePath { get; } = Path.Combine(Root, "FoodExpress.db3");

    /// <summary>
    /// Windows dev builds keep the database inside the project so it is easy to
    /// find. Android (and any build where the project folder can't be found)
    /// uses the normal per-app data folder.
    /// </summary>
    private static string ResolveRoot()
    {
#if WINDOWS
        string? projectFolder = FindProjectFolder();
        if (projectFolder is not null)
            return Path.Combine(projectFolder, "Data", "FoodExpressDb");
#endif

        return LegacyRoot;
    }

#if WINDOWS
    // bin\Debug\net9.0-windows...\win10-x64 -> walk up to the folder with the .csproj
    private static string? FindProjectFolder()
    {
        try
        {
            DirectoryInfo? folder = new(AppContext.BaseDirectory);

            while (folder is not null)
            {
                if (File.Exists(Path.Combine(folder.FullName, "FoodOrderingApp.csproj")))
                    return folder.FullName;

                folder = folder.Parent;
            }
        }
        catch
        {
            // fall back to the app data folder
        }

        return null;
    }
#endif

    private static SQLiteConnection Connection
    {
        get
        {
            if (_connection is not null)
                return _connection;

            _connection = new SQLiteConnection(
                DatabasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            return _connection;
        }
    }

    // ---------- Startup ----------

    /// <summary>
    /// Databases created before the admin dashboard existed have no IsAdmin
    /// column. CreateTable normally adds it, but make that explicit so an old
    /// .db3 file can never break sign-in.
    /// </summary>
    private static void EnsureUserAdminColumn(SQLiteConnection connection)
    {
        List<TableColumn> columns = connection.Query<TableColumn>("PRAGMA table_info(\"User\")");

        bool hasColumn = columns.Any(c => string.Equals(c.Name, "IsAdmin", StringComparison.OrdinalIgnoreCase));

        if (!hasColumn)
            connection.Execute("ALTER TABLE \"User\" ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0");
    }

    public static void Initialize()
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Root);

            bool isNew = !File.Exists(DatabasePath);

            SQLitePCL.Batteries_V2.Init();

            SQLiteConnection connection = Connection;

            connection.CreateTable<User>();
            EnsureUserAdminColumn(connection);
            connection.CreateTable<Food>();
            connection.CreateTable<FoodCategory>();
            connection.CreateTable<CartLine>();
            connection.CreateTable<Order>();
            connection.CreateTable<OrderItem>();
            connection.CreateTable<Setting>();

            if (isNew)
                ImportLegacyJson(connection);

            SyncSeed(connection);
        }
    }

    /// <summary>Imports the old users.json / users/&lt;id&gt;/*.json files once.</summary>
    private static void ImportLegacyJson(SQLiteConnection connection)
    {
        try
        {
            List<User>? users = ReadJson<List<User>>(Path.Combine(Root, "users.json"));
            if (users is null or { Count: 0 })
                return;

            connection.RunInTransaction(() =>
            {
                foreach (User user in users)
                {
                    connection.InsertOrReplace(user);

                    string userFolder = Path.Combine(Root, "users", user.Id);

                    List<CartLine>? cart = ReadJson<List<CartLine>>(Path.Combine(userFolder, "cart.json"));
                    if (cart is not null)
                    {
                        foreach (CartLine line in cart)
                        {
                            line.Id = 0;
                            line.UserId = user.Id;
                            connection.Insert(line);
                        }
                    }

                    List<Order>? orders = ReadJson<List<Order>>(Path.Combine(userFolder, "orders.json"));
                    if (orders is not null)
                    {
                        foreach (Order order in orders)
                        {
                            order.UserId = user.Id;
                            connection.Insert(order);

                            foreach (OrderItem item in order.Items)
                            {
                                item.Id = 0;
                                item.OrderId = order.Id;
                                connection.Insert(item);
                            }
                        }
                    }
                }

                SessionFile? session = ReadJson<SessionFile>(Path.Combine(Root, "session.json"));
                if (!string.IsNullOrWhiteSpace(session?.UserId))
                    SetSetting(connection, SessionKey, session!.UserId!);
            });
        }
        catch
        {
            // A partial import is fine: the seed still fills the menu and the
            // user can sign in again.
        }
    }

    /// <summary>
    /// Fills the menu from the JSON files packed into the app. Re-imports when a
    /// rebuild ships a different seed.
    /// </summary>
    private static void SyncSeed(SQLiteConnection connection)
    {
        byte[] foods = ReadBundledAsset("seed/foods.json");
        byte[] categories = ReadBundledAsset("seed/categories.json");

        if (foods.Length == 0 && categories.Length == 0)
            return;

        string fingerprint = Convert.ToHexString(SHA256.HashData(Combine(foods, categories)));
        string? last = GetSetting(connection, SeedHashKey);
        bool changed = !string.Equals(last, fingerprint, StringComparison.OrdinalIgnoreCase);

        if (!changed && connection.Table<Food>().Count() > 0)
            return;

        List<Food>? foodList = foods.Length > 0 ? Deserialize<List<Food>>(foods) : null;
        List<FoodCategory>? categoryList = categories.Length > 0 ? Deserialize<List<FoodCategory>>(categories) : null;

        if (foodList is null or { Count: 0 })
            foodList = SeedData.Foods();

        if (categoryList is null or { Count: 0 })
            categoryList = SeedData.Categories();

        connection.RunInTransaction(() =>
        {
            connection.DeleteAll<Food>();
            connection.InsertAll(foodList);

            connection.DeleteAll<FoodCategory>();
            connection.InsertAll(categoryList);
        });

        SetSetting(connection, SeedHashKey, fingerprint);
    }

    // ---------- Menu ----------

    public static List<Food> GetFoods()
    {
        lock (Gate)
            return Connection.Table<Food>().OrderBy(f => f.Id).ToList();
    }

    public static List<FoodCategory> GetCategories()
    {
        lock (Gate)
            return Connection.Table<FoodCategory>().ToList();
    }

    public static void SaveCategories(IEnumerable<FoodCategory> categories)
    {
        lock (Gate)
        {
            List<FoodCategory> list = categories.ToList();

            Connection.RunInTransaction(() =>
            {
                Connection.DeleteAll<FoodCategory>();
                Connection.InsertAll(list);
            });
        }
    }

    public static void SaveFoods(IEnumerable<Food> foods)
    {
        lock (Gate)
        {
            List<Food> list = foods.ToList();

            Connection.RunInTransaction(() =>
            {
                Connection.DeleteAll<Food>();
                Connection.InsertAll(list);
            });
        }
    }

    // ---------- Accounts ----------

    public static int UserCount()
    {
        lock (Gate)
            return Connection.Table<User>().Count();
    }

    /// <summary>Every registered account, oldest first (admin dashboard).</summary>
    public static List<User> GetUsers()
    {
        lock (Gate)
            return Connection.Table<User>()
                             .OrderBy(u => u.CreatedAt)
                             .ToList();
    }

    public static User? FindUser(string id)
    {
        lock (Gate)
            return Connection.Find<User>(id);
    }

    public static User? FindUserByEmail(string email)
    {
        lock (Gate)
        {
            // Small table; comparing in memory keeps the match case-insensitive
            // on every platform.
            return Connection.Table<User>()
                             .ToList()
                             .FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void InsertUser(User user)
    {
        lock (Gate)
            Connection.InsertOrReplace(user);
    }

    // ---------- Cart ----------

    public static List<CartLine> GetCart(string userId)
    {
        lock (Gate)
            return Connection.Table<CartLine>().Where(c => c.UserId == userId).ToList();
    }

    public static void SaveCart(string userId, IEnumerable<CartLine> lines)
    {
        lock (Gate)
        {
            List<CartLine> list = lines.ToList();

            Connection.RunInTransaction(() =>
            {
                Connection.Execute("DELETE FROM CartLine WHERE UserId = ?", userId);

                foreach (CartLine line in list)
                {
                    line.Id = 0;
                    line.UserId = userId;
                    Connection.Insert(line);
                }
            });
        }
    }

    // ---------- Orders ----------

    public static List<Order> GetOrders(string userId)
    {
        lock (Gate)
        {
            List<Order> orders = Connection.Table<Order>().Where(o => o.UserId == userId).ToList();
            AttachItems(orders);

            return orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToList();
        }
    }

    /// <summary>Every order from every customer (admin dashboard).</summary>
    public static List<Order> GetAllOrders()
    {
        lock (Gate)
        {
            List<Order> orders = Connection.Table<Order>().ToList();
            AttachItems(orders);

            return orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToList();
        }
    }

    /// <summary>Fills each order's Items list from the OrderItem table.</summary>
    private static void AttachItems(List<Order> orders)
    {
        if (orders.Count == 0) return;

        Dictionary<int, List<OrderItem>> items = Connection.Table<OrderItem>()
            .ToList()
            .GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (Order order in orders)
            order.Items = items.TryGetValue(order.Id, out List<OrderItem>? list)
                ? list
                : new List<OrderItem>();
    }

    public static void InsertOrder(Order order)
    {
        lock (Gate)
        {
            Connection.RunInTransaction(() =>
            {
                Connection.Insert(order);

                foreach (OrderItem item in order.Items)
                {
                    item.Id = 0;
                    item.OrderId = order.Id;
                    Connection.Insert(item);
                }
            });
        }
    }

    public static void UpdateOrder(Order order)
    {
        lock (Gate)
            Connection.Update(order);
    }

    public static void DeleteOrder(int orderId)
    {
        lock (Gate)
        {
            Connection.RunInTransaction(() =>
            {
                Connection.Execute("DELETE FROM OrderItem WHERE OrderId = ?", orderId);
                Connection.Delete<Order>(orderId);
            });
        }
    }

    /// <summary>Order numbers are unique across all users.</summary>
    public static int NextOrderId()
    {
        lock (Gate)
        {
            int max = Connection.ExecuteScalar<int>("SELECT COALESCE(MAX(Id), 1000) FROM \"Order\"");
            return max < 1000 ? 1001 : max + 1;
        }
    }

    // ---------- Settings ----------

    public static string? GetSetting(string key)
    {
        lock (Gate)
            return GetSetting(Connection, key);
    }

    public static void SetSetting(string key, string value)
    {
        lock (Gate)
            SetSetting(Connection, key, value);
    }

    public static void DeleteSetting(string key)
    {
        lock (Gate)
            Connection.Delete<Setting>(key);
    }

    private static string? GetSetting(SQLiteConnection connection, string key) =>
        connection.Find<Setting>(key)?.Value;

    private static void SetSetting(SQLiteConnection connection, string key, string value) =>
        connection.InsertOrReplace(new Setting { Key = key, Value = value });

    // ---------- JSON helpers (migration + seed) ----------

    private static T? ReadJson<T>(string path)
    {
        try
        {
            if (!File.Exists(path))
                return default;

            string json = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static T? Deserialize<T>(byte[] json)
    {
        try
        {
            ReadOnlySpan<byte> span = json;

            // Notepad and PowerShell like to add a UTF-8 BOM, and
            // System.Text.Json rejects it. Skip it so a hand-edited
            // foods.json still loads.
            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
                span = span[3..];

            return JsonSerializer.Deserialize<T>(span, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static byte[] ReadBundledAsset(string assetName)
    {
        try
        {
            // OpenAppPackageFileAsync completes synchronously, but run it on the
            // pool so startup can never deadlock on the UI thread.
            using Stream? stream = Task.Run(() => FileSystem.OpenAppPackageFileAsync(assetName))
                                          .GetAwaiter().GetResult();

            if (stream is null)
                return Array.Empty<byte>();

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private sealed class SessionFile
    {
        public string? UserId { get; set; }
    }

    /// <summary>One row of PRAGMA table_info(...).</summary>
    private sealed class TableColumn
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
