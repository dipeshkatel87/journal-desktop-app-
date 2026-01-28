using SQLite;

namespace MauiApp1.Models;

public class Mood
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Type { get; set; } = "Neutral"; // Positive/Neutral/Negative
}
