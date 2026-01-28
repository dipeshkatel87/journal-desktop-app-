using MauiApp1.Models;
using SQLite;
using Microsoft.Maui.Storage;

namespace MauiApp1.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private bool _inited;

    private async Task InitAsync()
    {
        if (_inited) return;

        var path = Path.Combine(FileSystem.AppDataDirectory, "journal.db");
        _db = new SQLiteAsyncConnection(path);

        await _db.CreateTableAsync<JournalEntry>();
        await _db.CreateTableAsync<Mood>();
        await _db.CreateTableAsync<Tag>();
        await _db.CreateTableAsync<EntryTag>();
        await _db.CreateTableAsync<AppSetting>();

        await SeedAsync();
        _inited = true;
    }

    private async Task SeedAsync()
    {
        if (await _db!.Table<Mood>().CountAsync() == 0)
        {
            await _db.InsertAllAsync(new[]
            {
                new Mood { Name="Happy", Type="Positive" },
                new Mood { Name="Excited", Type="Positive" },
                new Mood { Name="Calm", Type="Neutral" },
                new Mood { Name="Okay", Type="Neutral" },
                new Mood { Name="Sad", Type="Negative" },
                new Mood { Name="Angry", Type="Negative" },
            });
        }

        if (await _db!.Table<Tag>().CountAsync() == 0)
        {
            await _db.InsertAllAsync(new[]
            {
                new Tag { Name="Work", IsPredefined=true },
                new Tag { Name="Study", IsPredefined=true },
                new Tag { Name="Health", IsPredefined=true },
                new Tag { Name="Travel", IsPredefined=true },
                new Tag { Name="Fitness", IsPredefined=true },
                new Tag { Name="Family", IsPredefined=true },
            });
        }
    }

    // ---------- Lookups ----------
    public async Task<List<Mood>> GetMoodsAsync()
    {
        await InitAsync();
        return await _db!.Table<Mood>().OrderBy(m => m.Type).ThenBy(m => m.Name).ToListAsync();
    }

    public async Task<List<Tag>> GetTagsAsync()
    {
        await InitAsync();
        return await _db!.Table<Tag>().OrderBy(t => t.Name).ToListAsync();
    }

    // ---------- One entry/day ----------
    public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
    {
        await InitAsync();
        var d = date.Date;
        return await _db!.Table<JournalEntry>().FirstOrDefaultAsync(e => e.EntryDate == d);
    }

    public async Task SaveOrUpdateAsync(JournalEntry entry, List<string> tagNames)
    {
        await InitAsync();
        entry.EntryDate = entry.EntryDate.Date;

        var existing = await GetEntryByDateAsync(entry.EntryDate);

        if (existing == null)
        {
            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;
            await _db!.InsertAsync(entry);
            existing = entry;
        }
        else
        {
            existing.Title = entry.Title;
            existing.Content = entry.Content;
            existing.ContentType = entry.ContentType;
            existing.PrimaryMoodId = entry.PrimaryMoodId;
            existing.SecondaryMood1Id = entry.SecondaryMood1Id;
            existing.SecondaryMood2Id = entry.SecondaryMood2Id;
            existing.UpdatedAt = DateTime.Now;

            await _db!.UpdateAsync(existing);

            // remove old tags
            var oldMaps = await _db.Table<EntryTag>().Where(x => x.EntryId == existing.Id).ToListAsync();
            foreach (var m in oldMaps)
                await _db.DeleteAsync(m);
        }

        // add tags + mapping
        foreach (var name in tagNames.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
        {
            var tag = await _db!.Table<Tag>().FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
            if (tag == null)
            {
                tag = new Tag { Name = name, IsPredefined = false };
                await _db.InsertAsync(tag);
            }

            await _db.InsertAsync(new EntryTag { EntryId = existing.Id, TagId = tag.Id });
        }
    }

    public async Task DeleteAsync(DateTime date)
    {
        await InitAsync();
        var entry = await GetEntryByDateAsync(date);
        if (entry == null) return;

        var maps = await _db!.Table<EntryTag>().Where(x => x.EntryId == entry.Id).ToListAsync();
        foreach (var m in maps)
            await _db.DeleteAsync(m);

        await _db.DeleteAsync(entry);
    }

    // ---------- Timeline + search ----------
    public async Task<List<JournalEntry>> SearchAsync(string keyword)
    {
        await InitAsync();
        keyword = (keyword ?? "").Trim().ToLower();

        var all = await _db!.Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(keyword)) return all;

        return all.Where(e =>
            (e.Title ?? "").ToLower().Contains(keyword) ||
            (e.Content ?? "").ToLower().Contains(keyword)
        ).ToList();
    }

    // ---------- Streak ----------
    public async Task<(int current, int longest, int missed)> GetStreakAsync()
    {
        await InitAsync();
        var entries = await _db!.Table<JournalEntry>().OrderBy(e => e.EntryDate).ToListAsync();

        if (entries.Count == 0) return (0, 0, 0);

        var dates = entries.Select(e => e.EntryDate.Date).Distinct().OrderBy(d => d).ToList();

        int longest = 1, temp = 1, missed = 0;

        for (int i = 1; i < dates.Count; i++)
        {
            int diff = (dates[i] - dates[i - 1]).Days;
            if (diff == 1)
            {
                temp++;
                longest = Math.Max(longest, temp);
            }
            else
            {
                missed += Math.Max(0, diff - 1);
                temp = 1;
            }
        }

        // current streak (only if last entry is today)
        int current = 0;
        if (dates.Last() == DateTime.Today)
        {
            current = 1;
            for (int i = dates.Count - 1; i > 0; i--)
            {
                if ((dates[i] - dates[i - 1]).Days == 1) current++;
                else break;
            }
        }

        return (current, longest, missed);
    }

    // ---------- Settings ----------
    public async Task<string?> GetSettingAsync(string key)
    {
        await InitAsync();
        var s = await _db!.Table<AppSetting>().FirstOrDefaultAsync(x => x.Key == key);
        return s?.Value;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await InitAsync();
        await _db!.InsertOrReplaceAsync(new AppSetting { Key = key, Value = value });
    }

    // ---------- Export (Date range) ----------
    public async Task<List<JournalEntry>> GetEntriesBetweenAsync(DateTime from, DateTime to)
    {
        await InitAsync();

        from = from.Date;
        to = to.Date;

        return await _db!.Table<JournalEntry>()
            .Where(e => e.EntryDate >= from && e.EntryDate <= to)
            .OrderBy(e => e.EntryDate)
            .ToListAsync();
    }
    public async Task<List<string>> GetTagNamesForEntryAsync(int entryId)
    {
        await InitAsync();

        var maps = await _db!.Table<EntryTag>()
            .Where(x => x.EntryId == entryId)
            .ToListAsync();

        if (maps.Count == 0) return new List<string>();

        var tagIds = maps.Select(m => m.TagId).Distinct().ToHashSet();

        var tags = await _db.Table<Tag>().ToListAsync();

        return tags
            .Where(t => tagIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToList();
    }
    public async Task<MauiApp1.Models.DashboardData> GetDashboardDataAsync()
    {
        await InitAsync();

        var data = new MauiApp1.Models.DashboardData();

        // streak stats you already have
        var (current, longest, missed) = await GetStreakAsync();
        data.CurrentStreak = current;
        data.LongestStreak = longest;
        data.MissedDays = missed;

        var entries = await _db!.Table<JournalEntry>().ToListAsync();
        data.TotalEntries = entries.Count;

        if (entries.Count == 0)
            return data;

        var moods = await _db!.Table<Mood>().ToListAsync();
        var tags = await _db!.Table<Tag>().ToListAsync();
        var maps = await _db!.Table<EntryTag>().ToListAsync();

        // helper dictionaries
        var moodById = moods.ToDictionary(m => m.Id, m => m);
        var tagById = tags.ToDictionary(t => t.Id, t => t);

        // ---- Mood Distribution (Primary mood only) ----
        var typeCounts = new Dictionary<string, int>();

        foreach (var e in entries)
        {
            if (moodById.TryGetValue(e.PrimaryMoodId, out var mood))
            {
                var type = mood.Type ?? "Unknown";
                if (!typeCounts.ContainsKey(type)) typeCounts[type] = 0;
                typeCounts[type]++;
            }
        }

        foreach (var kv in typeCounts)
        {
            data.MoodPercent[kv.Key] = System.Math.Round(100.0 * kv.Value / entries.Count, 0);
        }

        // ---- Top Moods ----
        var moodCounts = new Dictionary<string, int>();

        foreach (var e in entries)
        {
            if (moodById.TryGetValue(e.PrimaryMoodId, out var mood))
            {
                var name = mood.Name ?? "Unknown";
                if (!moodCounts.ContainsKey(name)) moodCounts[name] = 0;
                moodCounts[name]++;
            }
        }

        data.TopMoods = moodCounts
            .OrderByDescending(x => x.Value)
            .Take(3)
            .Select(x => (x.Key, x.Value))
            .ToList();

        // ---- Top Tags ----
        var tagCounts = new Dictionary<int, int>();

        foreach (var m in maps)
        {
            if (!tagCounts.ContainsKey(m.TagId)) tagCounts[m.TagId] = 0;
            tagCounts[m.TagId]++;
        }

        data.TopTags = tagCounts
            .OrderByDescending(x => x.Value)
            .Take(6)
            .Select(x =>
            {
                var name = tagById.TryGetValue(x.Key, out var t) ? t.Name : $"Tag #{x.Key}";
                return (name, x.Value);
            })
            .ToList();

        return data;
    }

}
