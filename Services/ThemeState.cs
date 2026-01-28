namespace MauiApp1.Services;

public class ThemeState
{
    private readonly DatabaseService _db;

    public ThemeState(DatabaseService db)
    {
        _db = db;
    }

    public string CurrentTheme { get; private set; } = "light"; // "light" or "dark"
    public string CssClass => CurrentTheme == "dark" ? "theme-dark" : "";

    public event Action? OnChange;

    public async Task LoadAsync()
    {
        var t = await _db.GetSettingAsync("Theme");
        CurrentTheme = (t == "dark") ? "dark" : "light";
        OnChange?.Invoke();
    }

    public async Task SetAsync(string theme)
    {
        CurrentTheme = theme;
        await _db.SetSettingAsync("Theme", theme);
        OnChange?.Invoke();
    }
}
