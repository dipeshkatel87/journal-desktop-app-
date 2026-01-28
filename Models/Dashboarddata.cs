using System.Collections.Generic;

namespace MauiApp1.Models;

public class DashboardData
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int MissedDays { get; set; }
    public int TotalEntries { get; set; }

    // "Positive" -> 50, "Neutral" -> 30, "Negative" -> 20
    public Dictionary<string, double> MoodPercent { get; set; } = new();

    public List<(string Name, int Count)> TopMoods { get; set; } = new();
    public List<(string Name, int Count)> TopTags { get; set; } = new();
}
