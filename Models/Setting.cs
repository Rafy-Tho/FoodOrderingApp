using SQLite;

namespace FoodOrderingApp.Models;

/// <summary>Small key/value table used for the signed-in user and the seed fingerprint.</summary>
public class Setting
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
