namespace Plugin.Maui.ClipboardPlus.Tests;

public sealed class ExpirationTests
{
    [Fact]
    public async Task Expired_sensitive_clip_is_cleared()
    {
        var (clipboard, clock, _) = Harness.Create();
        ClipboardExpiredEventArgs? expired = null;
        clipboard.Expired += (_, args) => expired = args;

        await clipboard.SetSensitiveTextAsync("one-time-code", TimeSpan.FromMinutes(2));
        clock.Advance(TimeSpan.FromMinutes(2));
        await clipboard.ProcessDueExpirationsAsync();

        Assert.False(clipboard.HasText);
        Assert.NotNull(expired);
        Assert.Equal(ClipboardContentKind.Text, expired.Kind);
        Assert.Null(await clipboard.GetTextAsync());
    }

    [Fact]
    public async Task Expiration_does_not_clear_replaced_clip()
    {
        var (clipboard, clock, _) = Harness.Create();
        await clipboard.SetSensitiveTextAsync("old-secret", TimeSpan.FromMinutes(1));
        await clipboard.SetTextAsync("new public text");

        clock.Advance(TimeSpan.FromMinutes(5));
        await clipboard.ProcessDueExpirationsAsync();

        Assert.Equal("new public text", await clipboard.GetTextAsync());
    }

    [Fact]
    public async Task Expiration_does_not_clear_before_due()
    {
        var (clipboard, clock, _) = Harness.Create();
        await clipboard.SetSensitiveTextAsync("still-valid", TimeSpan.FromMinutes(2));

        clock.Advance(TimeSpan.FromMinutes(1));
        await clipboard.ProcessDueExpirationsAsync();

        Assert.Equal("still-valid", await clipboard.GetTextAsync());
        Assert.True(clipboard.HasSensitiveContent);
    }

    [Fact]
    public async Task Write_options_can_expire_non_sensitive_text()
    {
        var (clipboard, clock, _) = Harness.Create();
        await clipboard.SetTextAsync("promo", new ClipboardWriteOptions { Expiration = TimeSpan.FromSeconds(30) });

        clock.Advance(TimeSpan.FromSeconds(30));
        await clipboard.ProcessDueExpirationsAsync();

        Assert.False(clipboard.HasText);
    }
}
