namespace Plugin.Maui.ClipboardPlus.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 29, 2, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

static class Harness
{
    public static (ClipboardPlusImplementation Clipboard, FakeClock Clock, InMemoryClipboardPlatform Platform) Create(
        Action<ClipboardPlusOptions>? configure = null)
    {
        var clock = new FakeClock();
        var platform = new InMemoryClipboardPlatform();
        var options = new ClipboardPlusOptions
        {
            DefaultSensitiveExpiration = TimeSpan.FromMinutes(2),
            MonitorClipboard = true,
            RaiseCopiedEvent = true,
            ClearSensitiveOnAppBackground = false
        };
        configure?.Invoke(options);
        var clipboard = ClipboardPlus.Create(options, platform, clock);
        clipboard.Start();
        return (clipboard, clock, platform);
    }
}
