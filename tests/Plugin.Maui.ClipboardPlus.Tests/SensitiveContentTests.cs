namespace Plugin.Maui.ClipboardPlus.Tests;

public sealed class SensitiveContentTests
{
    [Fact]
    public async Task SetSensitiveTextAsync_redacts_preview_and_marks_sensitive()
    {
        var (clipboard, clock, _) = Harness.Create();

        var result = await clipboard.SetSensitiveTextAsync("otp-123456", TimeSpan.FromMinutes(2));

        Assert.True(result.IsSensitive);
        Assert.Null(result.Preview);
        Assert.Contains("sensitive", result.Confirmation, StringComparison.OrdinalIgnoreCase);
        Assert.True(clipboard.HasSensitiveContent);
        Assert.Equal(clock.UtcNow.AddMinutes(2), result.ExpiresAt);
        Assert.Equal("otp-123456", await clipboard.GetTextAsync());
    }

    [Fact]
    public async Task SetSensitiveTextAsync_uses_default_expiration()
    {
        var (clipboard, clock, _) = Harness.Create(options =>
            options.DefaultSensitiveExpiration = TimeSpan.FromSeconds(45));

        var result = await clipboard.SetSensitiveTextAsync("token");

        Assert.Equal(clock.UtcNow.AddSeconds(45), result.ExpiresAt);
        Assert.True(clipboard.HasSensitiveContent);
    }

    [Fact]
    public async Task ClearSensitiveAsync_clears_only_owned_sensitive_clip()
    {
        var (clipboard, _, _) = Harness.Create();
        await clipboard.SetSensitiveTextAsync("secret");

        await clipboard.ClearSensitiveAsync();

        Assert.False(clipboard.HasText);
        Assert.False(clipboard.HasSensitiveContent);
    }

    [Fact]
    public async Task ClearSensitiveAsync_does_not_clear_plain_text()
    {
        var (clipboard, _, _) = Harness.Create();
        await clipboard.SetTextAsync("public");

        await clipboard.ClearSensitiveAsync();

        Assert.Equal("public", await clipboard.GetTextAsync());
    }
}
