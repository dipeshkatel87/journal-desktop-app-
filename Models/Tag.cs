using SQLite;

namespace MauiApp1.Models;

public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Name { get; set; } = "";

    public bool IsPredefined { get; set; }
}
