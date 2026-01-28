using SQLite;

namespace MauiApp1.Models;

public class AppSetting
{
    [PrimaryKey]
    public string Key { get; set; } = "";

    public string Value { get; set; } = "";
}
