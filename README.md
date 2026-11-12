# FoodExpress — .NET MAUI Food Ordering App

A cross-platform food ordering app built with **.NET 9 / .NET MAUI**. Customers can
create an account, browse and search a menu, build a cart, check out, and track
their order history. Everything — accounts, menu, carts and orders — is stored
locally in a **SQLite** database, so the app works fully offline.

The project is a teaching/demo app: there is no server, no payment gateway and no
network calls. The "backend" is a single SQLite file on the device.

---

## Table of contents

1. [What the app can do](#1-what-the-app-can-do)
2. [Screens](#2-screens)
3. [User workflow](#3-user-workflow)
4. [Tech stack](#4-tech-stack)
5. [Project structure](#5-project-structure)
6. [Architecture](#6-architecture)
7. [Database implementation](#7-database-implementation)
8. [Menu seed pipeline](#8-menu-seed-pipeline)
9. [Authentication & security](#9-authentication--security)
10. [Business rules](#10-business-rules)
11. [Building & running](#11-building--running)
12. [PC ↔ phone data sync](#12-pc--phone-data-sync)
13. [Inspecting the database](#13-inspecting-the-database)
14. [Customising the app](#14-customising-the-app)
15. [Troubleshooting](#15-troubleshooting)
16. [Known limitations](#16-known-limitations)

---

## 1. What the app can do

### Accounts
- Register with full name, email and password (validated).
- Sign in / sign out; the session survives app restarts.
- Passwords are salted and hashed with **PBKDF2-SHA256** (100,000 iterations).
- A demo account is created automatically on first run.
- Show/hide password toggle, inline error messages, "use demo account" shortcut.

### Menu
- Menu loaded from SQLite (`Food`, `FoodCategory`).
- **Category chips** (All, Burgers, Pizza, Asian, Drinks, Desserts) with icons.
- **Live search** by dish name, debounced by 300 ms so typing is never interrupted.
- **Popular** section driven by the `IsPopular` flag (hidden while filtering).
- Empty-state message when a search returns nothing.
- Per-dish card with photo, name, description, price and a one-tap **+** button.
- A toast confirms each add-to-cart ("Margherita Pizza added to cart").

### Cart
- One cart per user, persisted after **every** change (add, +, −, remove, clear).
- Quantity controls (1–99); pressing "−" at quantity 1 offers to remove the item.
- Remove and clear-cart both ask for confirmation.
- Live subtotal, delivery fee and total.
- Floating cart bar on the home page with item count and subtotal.
- Cart badge on the header cart icon.

### Checkout
- Pre-fills the customer name from the account.
- Collects phone number and delivery address with validation.
- Payment method: **Cash on Delivery**.
- Order summary with every line and the exact totals.
- Double-tap protection while the order is being placed.
- The order, its items and the cart clearing happen in **one database transaction**.

### Orders
- Order history, newest first, with status pill (Pending / Cancelled / Delivered colours).
- Full details per order: number, date, customer, phone, address, payment method,
  line items, subtotal, delivery fee, total.
- Summary header: number of orders and total spent (cancelled orders excluded).
- Cancel a **Pending** order (confirmation required).
- Delete an order from the history (confirmation required).
- Order numbers are globally unique and start at **#1001**.

### Account
- Profile page with avatar initial, full name and email.
- Live counters: number of orders and items in the cart.
- Admin accounts get an **Admin dashboard** shortcut (see below).
- Sign out (cart and orders stay saved for the next sign-in).

### Admin dashboard
- Only visible to accounts with `User.IsAdmin`.
- **Products**: add a dish (name, description, price, category, photo, popular),
  edit any existing dish (the form re-opens pre-filled) or remove dishes from
  the menu.
- **Categories**: add, rename (with a new icon) or delete filter chips, and see
  how many items each category holds. Renaming a category moves its dishes with
  it; deleting a category also removes its dishes.
- **Users**: browse every registered account with role and joined date.
- **Orders**: see all customers' orders, confirm a pending order as **Delivered**
  and watch pending count / delivered revenue update.

---

## 2. Screens

| Page | File | Purpose |
|---|---|---|
| Login | `Views/LoginPage.xaml` | Email + password sign-in, demo shortcut, link to register |
| Register | `Views/RegisterPage.xaml` | Create an account (name, email, password, confirm) |
| Home | `Views/HomePage.xaml` | Greeting, search, category chips, popular section, full menu, cart bar |
| Cart | `Views/CartPage.xaml` | Line items, quantity +/−, remove, clear, totals, checkout button |
| Checkout | `Views/CheckoutPage.xaml` | Delivery details, payment method, order summary, place order |
| Order history | `Views/OrderHistoryPage.xaml` | Order list, status, totals, cancel/delete |
| Account | `Views/AccountPage.xaml` | Profile, stats, admin dashboard link, sign out |
| Admin dashboard | `Views/AdminPage.xaml` | Add products/categories, view users, confirm orders |

Navigation is a plain `NavigationPage` (push/pop). Signing in or out swaps the
window's root page rather than navigating.

---

## 3. User workflow

```
┌─────────────┐   no session    ┌─────────────┐
│  App start  │ ──────────────► │  Login page │
└─────────────┘                 └──────┬──────┘
       │ session found                 │ sign in / register
       ▼                               ▼
┌─────────────────────────────────────────────────┐
│                   Home page                     │
│  greeting · search · categories · menu · cart   │
└───┬───────────────┬──────────────────┬──────────┘
    │ + add dish    │ cart bar / icon  │ orders icon / avatar
    ▼               ▼                  ▼
 (cart grows)   ┌──────────┐     ┌──────────────┐   ┌───────────┐
                │ Cart page│     │Order history │   │Account    │
                └────┬─────┘     └──────────────┘   └─────┬─────┘
                     │ checkout                          │ sign out
                     ▼                                   ▼
              ┌──────────────┐                     (back to Login)
              │Checkout page │
              └──────┬───────┘
                     │ place order (transaction)
                     ▼
         order saved · cart cleared · back to Home
```

Step by step:

1. **First launch** — the database is created, the menu is seeded and a demo
   account (`demo@foodexpress.com` / `demo123`) is created.
2. **Sign in or register** — registering signs you in immediately.
3. **Browse** — filter by category chip, search by name, or tap **+** on any dish.
4. **Cart** — adjust quantities, remove items, review totals.
5. **Checkout** — confirm name, enter phone and address, tap *Place Order*.
6. **Order placed** — a confirmation dialog shows the order number and total; the
   cart is emptied and you return to the home page.
7. **Order history** — review past orders, cancel a pending one, or delete it.
8. **Account** — see your stats and the database location, or sign out.
9. **Next launch** — the session, cart and orders are restored from SQLite.

---

## 4. Tech stack

| Area | Choice |
|---|---|
| Framework | .NET 9 (`net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0`) |
| UI | .NET MAUI XAML + code-behind event handlers |
| Data access | `sqlite-net-pcl` **1.9.172** (synchronous API) |
| SQLite native | `SQLitePCLRaw.bundle_green` **2.1.11** |
| Logging | `Microsoft.Extensions.Logging.Debug` 9.0.8 |
| Fonts | OpenSans Regular/Semibold + Font Awesome 6 Free Solid (`FASolid`) |
| Serialization | `System.Text.Json` (menu seed + one-time JSON migration only) |
| Navigation | `NavigationPage` with push/pop and root swapping |
| Sync tooling | PowerShell + `adb` (`sync-data.ps1`) |

There is **no MVVM framework** and no DI container for services: the services are
static classes, and pages bind directly to `ObservableCollection`s. Models that
change at runtime (`Order`, `CartItem`) implement `INotifyPropertyChanged`.

---

## 5. Project structure

```
FoodOrderingApp/
├── App.xaml / App.xaml.cs          App startup, colour palette, root page swapping
├── AppShell.xaml                   (unused template shell)
├── MainPage.xaml                   (unused template page)
├── MauiProgram.cs                  Font registration, debug logging
├── FoodOrderingApp.csproj          Targets, packages, seed assets
├── sync-data.ps1                   PC ↔ Android database sync
│
├── Models/
│   ├── User.cs                     Account (Id, name, email, hash, salt, createdAt)
│   ├── Food.cs                     Menu item (Id, name, price, category, image…)
│   ├── FoodCategory.cs             Category chip (Name, IconKey)
│   ├── CartLine.cs                 One cart row (UserId, FoodId, Quantity)
│   ├── CartItem.cs                 Runtime cart view-model (Food + Quantity)
│   ├── Order.cs                    Order header + display helpers + status colours
│   ├── OrderItem.cs                One line of an order
│   └── Setting.cs                  Key/value table (session, seed hash)
│
├── Services/
│   ├── Database.cs                 SQLite: path, schema, migration, seed, queries
│   ├── MenuService.cs              In-memory menu + search/filter + CRUD helpers
│   ├── AuthService.cs              Register/login/logout, PBKDF2 hashing
│   ├── CartService.cs              Cart operations + totals
│   ├── OrderService.cs             Place/cancel/delete orders
│   └── SeedData.cs                 Hard-coded fallback menu
│
├── Views/
│   ├── LoginPage.xaml(.cs)
│   ├── RegisterPage.xaml(.cs)
│   ├── HomePage.xaml(.cs)
│   ├── CartPage.xaml(.cs)
│   ├── CheckoutPage.xaml(.cs)
│   ├── OrderHistoryPage.xaml(.cs)
│   └── AccountPage.xaml(.cs)
│
├── Controls/PageHeader.cs          Reusable header (title, back, cart/orders chips, avatar)
├── Icons.cs                        Font Awesome glyphs used in code
├── CategoryIcons.cs                Category name → icon glyph
│
├── Resources/
│   ├── Images/food_*.jpg          Dish photos
│   ├── Fonts/                     OpenSans + fa-solid-900.ttf
│   └── Styles/                    Colours and global styles
│
└── Data/FoodExpressDb/
    ├── foods.json                  Menu seed (bundled into the app)
    ├── categories.json             Category seed (bundled into the app)
    ├── FoodExpress.db3             SQLite database (created at runtime)
    └── users.json, session.json, users/<id>/…   Legacy JSON backup (ignored)
```

---

## 6. Architecture

```
        ┌──────────────────────────────────────────────┐
        │                 Views (XAML)                 │
        │  Login · Register · Home · Cart · Checkout   │
        │  Order history · Account                     │
        └───────────────────────┬──────────────────────┘
                                │ calls
        ┌───────────────────────▼──────────────────────┐
        │                  Services                    │
        │  AuthService   MenuService   CartService     │
        │  OrderService                                │
        └───────────────────────┬──────────────────────┘
                                │ calls
        ┌───────────────────────▼──────────────────────┐
        │              Database (static)               │
        │  one SQLiteConnection + lock, transactions   │
        └───────────────────────┬──────────────────────┘
                                │
                    ┌───────────▼───────────┐
                    │  FoodExpress.db3      │
                    │  (SQLite, 7 tables)   │
                    └───────────────────────┘
```

**Startup sequence** (`App` constructor + `CreateWindow`):

1. `Database.Initialize()` — create folder, open connection, `CreateTable` ×7,
   import legacy JSON (first run only), sync the menu seed.
2. `MenuService.Initialize()` — load `Food` / `FoodCategory` rows into
   `ObservableCollection`s, prepend the synthetic **All** chip.
3. `AuthService.Initialize()` — ensure the demo account exists, restore the
   session from `Setting["CurrentUserId"]`.
4. `CreateWindow` — if signed in, load the user's cart and orders; root page is
   `HomePage`, otherwise `LoginPage`.
5. On Windows the window is sized to a phone-like 480 × 860.

Services are synchronous. The only background work is password hashing, which
runs on the thread pool from the login/register pages so the UI stays responsive.

---

## 7. Database implementation

### 7.1 File locations

| Platform | Path |
|---|---|
| **Windows (dev)** | `<project>\Data\FoodExpressDb\FoodExpress.db3` |
| **Android** | `/data/user/0/com.companyname.foodorderingapp/files/FoodExpressDb/FoodExpress.db3` |
| **iOS / MacCatalyst** | `FileSystem.AppDataDirectory/FoodExpressDb/FoodExpress.db3` |

On Windows the database deliberately lives inside the project folder so it is easy
to find, inspect and back up. On every other platform it uses the normal per-app
data directory.

The path is resolved once in `Database.ResolveRoot()`:

```csharp
#if WINDOWS
    // walk up from bin\Debug\net9.0-windows…\win10-x64 to the folder with the .csproj
    string? projectFolder = FindProjectFolder();
    if (projectFolder is not null)
        return Path.Combine(projectFolder, "Data", "FoodExpressDb");
#endif
    return Path.Combine(FileSystem.AppDataDirectory, "FoodExpressDb");
```

### 7.2 Connection setup

```csharp
_connection = new SQLiteConnection(
    DatabasePath,
    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
```

- **One** connection for the whole app, created lazily.
- Every public `Database` method takes a single `lock (Gate)`, so the UI thread
  and any background work never use the connection concurrently.
- `SQLitePCL.Batteries_V2.Init()` is called during initialisation to register the
  native SQLite provider (`bundle_green`).
- `CreateTable<T>()` runs for all seven models on every start. It is idempotent
  and also adds any **new columns** to an existing table, which gives the app a
  simple forward-only schema migration.

### 7.3 Schema

All tables use sqlite-net's default mapping: class name = table name, property
name = column name.

#### `User`

| Column | Type | Notes |
|---|---|---|
| `Id` | TEXT | **PK**, GUID (`"N"` format) |
| `FullName` | TEXT | |
| `Email` | TEXT | **unique index** `User_Email` |
| `PasswordHash` | TEXT | Base64 PBKDF2-SHA256 hash |
| `PasswordSalt` | TEXT | Base64, 16 random bytes |
| `CreatedAt` | INTEGER | `DateTime` stored as ticks |

#### `Food`

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK** (from the seed JSON, not auto-increment) |
| `Name` | TEXT | |
| `Description` | TEXT | |
| `Price` | REAL | `decimal` |
| `Category` | TEXT | matches `FoodCategory.Name` |
| `Image` | TEXT | bundled image file name, e.g. `food_margherita.jpg` |
| `IsPopular` | INTEGER | boolean, drives the Popular section |

#### `FoodCategory`

| Column | Type | Notes |
|---|---|---|
| `Name` | TEXT | **PK** (`Burgers`, `Pizza`, …) |
| `IconKey` | TEXT | `burgers`, `pizza`, `asian`, `drinks`, `desserts` |

The **All** chip is not stored — it is created in `MenuService` and prepended to
the collection.

#### `CartLine`

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK**, auto-increment |
| `UserId` | TEXT | **index** `CartLine_UserId` |
| `FoodId` | INTEGER | references `Food.Id` |
| `Quantity` | INTEGER | 1–99 |

One row per (user, dish). The cart is replaced as a whole on every save.

#### `Order`

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK**, assigned by the app (starts at 1001) |
| `UserId` | TEXT | **index** `Order_UserId` |
| `CustomerName` | TEXT | |
| `PhoneNumber` | TEXT | |
| `DeliveryAddress` | TEXT | |
| `PaymentMethod` | TEXT | `"Cash on Delivery"` |
| `Subtotal` | REAL | |
| `DeliveryFee` | REAL | |
| `Total` | REAL | |
| `OrderDate` | INTEGER | `DateTime` ticks |
| `Status` | TEXT | `Pending`, `Cancelled`, `Delivered` |

> `Order` is a SQL keyword, so raw SQL must quote it: `SELECT * FROM "Order";`
> sqlite-net already quotes table names in its generated SQL.

#### `OrderItem`

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK**, auto-increment |
| `OrderId` | INTEGER | **index** `OrderItem_OrderId` |
| `FoodName` | TEXT | name copied at order time |
| `Image` | TEXT | image copied at order time |
| `UnitPrice` | REAL | price copied at order time |
| `Quantity` | INTEGER | |

Line items are **snapshots**: renaming or repricing a dish later does not change
historical orders.

#### `Setting`

| Column | Type | Notes |
|---|---|---|
| `Key` | TEXT | **PK** |
| `Value` | TEXT | |

Keys currently used:

| Key | Meaning |
|---|---|
| `CurrentUserId` | Signed-in user id (session). Removed on sign-out. |
| `SeedHash` | SHA-256 of the bundled menu seed last imported |

### 7.4 Relationships

```
User 1 ──── * CartLine        (CartLine.UserId)
User 1 ──── * Order           (Order.UserId)
Order 1 ─── * OrderItem       (OrderItem.OrderId)
Food 1 ──── * CartLine        (CartLine.FoodId, logical only)
FoodCategory 1 ── * Food      (Food.Category, logical only)
```

Only `CartLine`, `Order` and `OrderItem` carry indexes. There are no SQL foreign
keys — the relationships are enforced in code. Deleting an order deletes its
items in the same transaction.

### 7.5 Initialisation flow

```csharp
public static void Initialize()
{
    lock (Gate)
    {
        Directory.CreateDirectory(Root);
        bool isNew = !File.Exists(DatabasePath);

        SQLitePCL.Batteries_V2.Init();

        var connection = Connection;
        connection.CreateTable<User>();
        connection.CreateTable<Food>();
        connection.CreateTable<FoodCategory>();
        connection.CreateTable<CartLine>();
        connection.CreateTable<Order>();
        connection.CreateTable<OrderItem>();
        connection.CreateTable<Setting>();

        if (isNew)
            ImportLegacyJson(connection);   // one-time migration from the JSON version

        SyncSeed(connection);              // menu from the bundled JSON
    }
}
```

**First run only** (`isNew`) the old JSON files are imported:

```
Data\FoodExpressDb\
├── users.json                    → User rows
├── session.json                  → Setting["CurrentUserId"]
└── users\<userId>\
    ├── cart.json                 → CartLine rows
    └── orders.json               → Order + OrderItem rows
```

The whole import runs inside `RunInTransaction`, so a crash cannot leave a
half-imported account behind. The JSON files are then left on disk as a backup
and ignored forever after.

### 7.6 Transactions

| Operation | What happens in the transaction |
|---|---|
| Legacy import | All users + carts + orders + session |
| Menu seed | `DELETE` all `Food` / `FoodCategory`, then insert the new rows |
| `SaveCart` | `DELETE FROM CartLine WHERE UserId = ?`, then insert the new lines |
| `InsertOrder` | Insert the `Order`, then every `OrderItem` |
| `DeleteOrder` | Delete the order's `OrderItem`s, then the `Order` |

This is why placing an order can never leave an order without items, or clear a
cart without saving the order.

### 7.7 Type notes

- `DateTime` is stored as **ticks** (`bigint`). sqlite-net's default
  `storeDateTimeAsTicks = true`.
- `decimal` (prices) is stored as SQLite **REAL**. It round-trips correctly for
  normal prices (`5.99`, `2.50`), but it is floating point — for
  accounting-grade money you would store integer cents instead.
- Booleans are stored as `0` / `1` integers.

### 7.8 Useful queries

```sql
-- menu
SELECT Id, Name, Price, Category FROM Food ORDER BY Id;

-- who is signed in
SELECT Value FROM Setting WHERE Key = 'CurrentUserId';

-- a user's cart with dish names
SELECT c.FoodId, f.Name, c.Quantity, f.Price * c.Quantity AS LineTotal
FROM CartLine c JOIN Food f ON f.Id = c.FoodId;

-- orders with item counts
SELECT o.Id, o.Status, o.Total, COUNT(i.Id) AS Items
FROM "Order" o LEFT JOIN OrderItem i ON i.OrderId = o.Id
GROUP BY o.Id ORDER BY o.Id DESC;

-- one order in full
SELECT * FROM "Order" WHERE Id = 1001;
SELECT FoodName, UnitPrice, Quantity FROM OrderItem WHERE OrderId = 1001;
```

---

## 8. Menu seed pipeline

The menu is **not** hard-coded in C#. It comes from two JSON files in the project:

```
Data\FoodExpressDb\foods.json
Data\FoodExpressDb\categories.json
```

They are packed into the app as MAUI assets:

```xml
<MauiAsset Include="Data\FoodExpressDb\foods.json"      LogicalName="seed/foods.json" />
<MauiAsset Include="Data\FoodExpressDb\categories.json" LogicalName="seed/categories.json" />
```

At startup `SyncSeed()`:

1. Reads `seed/foods.json` and `seed/categories.json` from the app package.
2. Computes `SHA-256(foods.json + categories.json)`.
3. Compares it with `Setting["SeedHash"]`.
4. If the hash differs (or the `Food` table is empty), it replaces the menu in one
   transaction and stores the new hash.

Consequences:

- **Edit the JSON, rebuild, run** → the new menu appears on Windows *and* Android.
- **Edits made directly in the database survive** normal restarts, because the
  seed is only re-imported when the bundled files change.
- If the asset is missing or unreadable, `SeedData.Foods()` /
  `SeedData.Categories()` (hard-coded copies) are used as a fallback.
- A UTF-8 BOM at the start of the JSON is skipped, so files saved by Notepad or
  PowerShell still load.

> The files are copied into the app package at **build time**. Changing
> `foods.json` without rebuilding will not change the running app.

---

## 9. Authentication & security

- Passwords are **never** stored in plain text.
- Each account gets a random 16-byte salt.
- Hash: `Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, SHA256, 32)`.
- Verification uses `CryptographicOperations.FixedTimeEquals` (constant time).
- Email uniqueness is enforced by a **unique index** on `User.Email` and checked
  case-insensitively before insert.
- The session is a row in `Setting` (`CurrentUserId`); signing out deletes it.
- The demo account is created only when the `User` table is empty:

```
Email:    demo@foodexpress.com
Password: demo123
```

- A store-admin account is created on first run (and restored if missing):

```
Email:    admin@foodexpress.com
Password: admin123
```

Admins have `User.IsAdmin = true`, which reveals the Admin dashboard on the
account page. The dashboard is guarded in `AdminPage.OnAppearing()`.

This is a local demo app: there is no server, token, or transport security to
consider. The hash protects the password if someone opens the `.db3` file.

---

## 10. Business rules

| Rule | Value |
|---|---|
| Delivery fee | **$2.50** flat, whenever the cart has at least one item (`CartService.DeliveryFeeAmount`) |
| Total | subtotal + delivery fee |
| Cart quantity | 1 – 99 per line |
| Order number | `MAX(Order.Id) + 1`, starting at **1001**, unique across all users |
| New order status | `Pending` |
| Cancelling | only `Pending` orders (`Order.CanCancel`) |
| Total spent | sum of all non-cancelled orders |
| Payment | `Cash on Delivery` (single option) |
| Checkout validation | name ≥ 2 chars · phone 8–15 digits, only digits/space/`+`/`-`/`(`/`)` · address ≥ 10 chars |
| Registration validation | name ≥ 2 chars · valid email · password ≥ 6 chars · confirmation must match |
| Search | case-insensitive substring match on dish name, 300 ms debounce |
| Category filter | exact match on `Food.Category`; `All` means no filter |

---

## 11. Building & running

### Prerequisites

- .NET SDK **9** (the project is built with the .NET 9 MAUI workload)
- MAUI workloads: `dotnet workload install maui-android maui-windows` (or the
  Visual Studio "Mobile development with .NET" workload)
- Android: JDK 17, Android SDK + platform-tools, an emulator or device
- Windows: Windows 10 build 17763 or newer (the Windows target framework is
  `net9.0-windows10.0.19041.0`)

### Windows

```powershell
dotnet restore
dotnet build "FoodOrderingApp.csproj" -f net9.0-windows10.0.19041.0 -c Debug
dotnet run   -f net9.0-windows10.0.19041.0
```

The app opens in a phone-shaped 480 × 860 window. The database is created at
`Data\FoodExpressDb\FoodExpress.db3`.

### Android

```powershell
dotnet build "FoodOrderingApp.csproj" -f net9.0-android -c Debug
adb install -r "bin\Debug\net9.0-android\com.companyname.foodorderingapp-Signed.apk"
adb shell am start -n com.companyname.foodorderingapp/crc64cdf54960f53d0bee.MainActivity
```

If `adb` is not on `PATH`:

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
```

> **Debug APK note:** `FoodOrderingApp.csproj` sets
> `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` for Android Debug
> builds. Without it, a Debug APK installed by hand (instead of deployed by
> Visual Studio) starts to a **black screen** because the managed assemblies were
> never pushed to the device.

### iOS / MacCatalyst

The target frameworks are present, but building them requires a Mac with Xcode.
Nothing in the data layer is platform-specific, so they should run unchanged.

---

## 12. PC ↔ phone data sync

A PC and a phone cannot share one live SQLite file — they are different machines
with different sandboxes. `sync-data.ps1` moves the file with `adb`.

```powershell
.\sync-data.ps1 -Status   # show both copies, sizes, SHA-256 and whether they match
.\sync-data.ps1 -Pull     # Android -> project   (previous project copy saved as .bak)
.\sync-data.ps1 -Push     # project -> Android
```

What the script does:

- Finds `adb` (PATH, then the usual SDK locations).
- Verifies a device is connected (`adb get-state` = `device`).
- **Force-stops the app** before touching the file, and **restarts it** afterwards,
  so the app always reloads the new data.
- `-Pull` stops the Windows app too (it keeps the file open) and keeps a
  `FoodExpress.db3.bak` of the previous project copy.
- `-Push` copies into the app sandbox with `run-as`, fixes permissions (`600`)
  and removes stale `-wal` / `-shm` / `-journal` files.
- `-Status` hashes the Windows file through a shared read handle, so it works
  even while the Windows app is running.

Requirements: the Android build must be **debuggable** (`adb run-as` only works
on debug builds), and the app must have been opened at least once so its data
folder exists.

---

## 13. Inspecting the database

The file is a standard SQLite 3 database. Any SQLite tool works.

### DB Browser for SQLite (GUI)

```powershell
winget install DBBrowserForSQLite.DBBrowserForSQLite
```

Open `Data\FoodExpressDb\FoodExpress.db3` (switch the file filter to *All files*).
Use **Browse Data** to view/edit tables and **Execute SQL** for queries.
**Close the Windows app before writing** — otherwise the database is locked.

### VS Code

Install the *SQLite Viewer* extension and click the `.db3` file in the explorer
(read-only browsing).

### Python (already on this machine)

```powershell
python -c "import sqlite3; c=sqlite3.connect(r'Data\FoodExpressDb\FoodExpress.db3'); print(list(c.execute('select Id,Name,Price from Food')))"
```

### Command line

```powershell
sqlite3 Data\FoodExpressDb\FoodExpress.db3 "select * from \"Order\";"
```

> If you edit data behind the app's back, **restart the app** — the menu, cart
> and orders are cached in memory while it runs.

---

## 14. Customising the app

| What | Where |
|---|---|
| Menu items / prices / photos | `Data\FoodExpressDb\foods.json` → rebuild |
| Categories and their icons | `Data\FoodExpressDb\categories.json` → rebuild |
| Category → icon mapping | `CategoryIcons.cs` |
| Delivery fee | `CartService.DeliveryFeeAmount` |
| Order number start | `Database.NextOrderId()` (currently 1001) |
| Colours and typography | `App.xaml` (palette + global styles) |
| Demo account | `AuthService.Initialize()` |
| Database location | `Database.ResolveRoot()` |
| Seed assets | `FoodOrderingApp.csproj` (`MauiAsset` entries) |

`MenuService` also exposes `AddFood`, `UpdateFood` and `DeleteFood` (with
`SaveFoods`) for a future admin screen; the current UI only reads the menu.

---

## 15. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Android shows a black screen after installing the APK by hand | Debug APK without embedded assemblies. Keep `EmbedAssembliesIntoApk=true` or deploy from Visual Studio. |
| "database is locked" when editing with DB Browser | The Windows app has the file open. Close the app (or use `-Pull`, which stops it). |
| Menu changes in `foods.json` do not appear | The JSON is packed at build time — rebuild, then run. The seed hash must change. |
| Database edits do not appear | The app caches data at startup — restart it. |
| `.\sync-data.ps1` says "no device" | Start the emulator/phone and check `adb devices`. |
| `run-as` fails | The installed APK is not debuggable (Release build). Install a Debug build. |
| Menu shows 14 dishes instead of 15 | Fixed: a UTF-8 BOM in `foods.json` used to make JSON parsing fail silently and fall back to `SeedData`. The reader now skips the BOM. |
| Emulator ignores `adb shell input text` | Gboard can swallow injected keys. Disable the IME for scripted tests: `adb shell ime disable com.google.android.inputmethod.latin/com.android.inputmethod.latin.LatinIME`, and re-enable it afterwards. |
| App data looks stale on the phone | `-Push` the project database, or `-Pull` the phone's copy. |

---

## 16. Known limitations

- **Local only** — no backend, no accounts across devices, no real payments.
- **Single payment method** — Cash on Delivery.
- **Manual sync** — the PC and phone databases are independent until you run
  `sync-data.ps1`; the last copy wins.
- **"Free delivery over $20"** on the home banner is marketing copy; the actual
  rule is a flat $2.50 delivery fee.
- **No admin UI** — `Delivered` is supported by the model and colours but nothing
  in the UI sets it. `MenuService` CRUD methods are unused.
- **Money as REAL** — fine for a demo, not for production accounting.
- **No automated tests** — the data layer is verified manually on Windows and
  Android.
- **Whole-file sync** — `-Push` replaces the phone's database entirely; it does
  not merge changes made on both sides.
- `AppShell.xaml` and `MainPage.xaml` are unused template leftovers; the app
  creates its window and root page directly in `App.xaml.cs`.
