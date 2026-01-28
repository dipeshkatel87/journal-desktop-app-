using SQLite;

namespace MauiApp1.Models;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // ✅ One entry per day
    [Indexed(Unique = true)]
    public DateTime EntryDate { get; set; }

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentType { get; set; } = "Markdown";

    // moods
    public int PrimaryMoodId { get; set; }
    public int? SecondaryMood1Id { get; set; }
    public int? SecondaryMood2Id { get; set; }

    // timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
