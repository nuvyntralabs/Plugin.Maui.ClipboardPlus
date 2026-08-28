namespace Plugin.Maui.ClipboardPlus.Tests;

public sealed class ClipboardPlusTests
{
    [Fact]
    public async Task SetTextAsync_sets_presence_and_returns_confirmation()
    {
        var (clipboard, _, _) = Harness.Create();

        var result = await clipboard.SetTextAsync("hello world");

        Assert.True(result.Succeeded);
        Assert.Equal(ClipboardContentKind.Text, result.Kind);
        Assert.Equal("Copied", result.Confirmation);
        Assert.Equal("hello world", result.Preview);
        Assert.True(clipboard.HasText);
        Assert.False(clipboard.HasImage);
        Assert.False(clipboard.HasUri);
        Assert.Equal("hello world", await clipboard.GetTextAsync());
    }

    [Fact]
    public async Task SetUriAsync_sets_has_uri()
    {
        var (clipboard, _, _) = Harness.Create();
        var uri = new Uri("https://niladripadhy.vercel.app/#opensource");

        var result = await clipboard.SetUriAsync(uri);

        Assert.Equal(ClipboardContentKind.Uri, result.Kind);
        Assert.True(clipboard.HasUri);
        Assert.True(clipboard.HasText);
        Assert.Equal(uri, await clipboard.GetUriAsync());
    }

    [Fact]
    public async Task SetImageAsync_sets_has_image()
    {
        var (clipboard, _, _) = Harness.Create();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var result = await clipboard.SetImageAsync(png, "image/png");

        Assert.Equal(ClipboardContentKind.Image, result.Kind);
        Assert.True(clipboard.HasImage);
        var image = await clipboard.GetImageAsync();
        Assert.NotNull(image);
        Assert.Equal(png, image.Data);
        Assert.Equal("image/png", image.MimeType);
    }

    [Fact]
    public async Task SetFilesAsync_sets_has_files()
    {
        var (clipboard, _, _) = Harness.Create();

        var result = await clipboard.SetFilesAsync(["/tmp/invoice.pdf", "/tmp/photo.jpg"]);

        Assert.Equal(ClipboardContentKind.Files, result.Kind);
        Assert.True(clipboard.HasFiles);
        var files = await clipboard.GetFilesAsync();
        Assert.Equal(2, files.Count);
        Assert.Equal("invoice.pdf", files[0].FileName);
    }

    [Fact]
    public async Task GetContentAsync_returns_text_and_presence()
    {
        var (clipboard, _, _) = Harness.Create();
        await clipboard.SetTextAsync("snippet");

        var content = await clipboard.GetContentAsync();

        Assert.True(content.Presence.HasText);
        Assert.Equal("snippet", content.Text);
        Assert.Null(content.Image);
    }

    [Fact]
    public async Task ClearAsync_empties_clipboard()
    {
        var (clipboard, _, _) = Harness.Create();
        await clipboard.SetTextAsync("temp");

        await clipboard.ClearAsync();

        Assert.False(clipboard.HasText);
        Assert.Null(await clipboard.GetTextAsync());
    }

    [Fact]
    public async Task ContentChanged_and_Copied_fire_on_write()
    {
        var (clipboard, _, _) = Harness.Create();
        ClipboardChangedEventArgs? changed = null;
        ClipboardCopiedEventArgs? copied = null;
        clipboard.ContentChanged += (_, args) => changed = args;
        clipboard.Copied += (_, args) => copied = args;

        await clipboard.SetTextAsync("ping");

        Assert.NotNull(changed);
        Assert.Equal(ClipboardChangeSource.Self, changed.Source);
        Assert.True(changed.Presence.HasText);
        Assert.NotNull(copied);
        Assert.Equal("Copied", copied.Result.Confirmation);
    }

    [Fact]
    public async Task Preview_is_truncated()
    {
        var (clipboard, _, _) = Harness.Create(options => options.PreviewMaxLength = 8);

        var result = await clipboard.SetTextAsync("abcdefghijk");

        Assert.Equal("abcdefgh…", result.Preview);
    }

    [Fact]
    public async Task Relative_uri_is_rejected()
    {
        var (clipboard, _, _) = Harness.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => clipboard.SetUriAsync(new Uri("/relative", UriKind.Relative)));
    }
}
