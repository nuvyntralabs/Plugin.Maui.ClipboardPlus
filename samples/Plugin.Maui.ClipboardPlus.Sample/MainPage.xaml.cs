using Plugin.Maui.ClipboardPlus;

namespace Plugin.Maui.ClipboardPlus.Sample;

public partial class MainPage : ContentPage
{
    readonly IClipboardPlus _clipboard;
    readonly List<string> _log = [];

    public MainPage(IClipboardPlus clipboard)
    {
        InitializeComponent();
        _clipboard = clipboard;
        _clipboard.ContentChanged += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            Log($"changed {args.Source} {Format(args.Presence)}");
            RefreshPresence();
        });
        _clipboard.Copied += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            ConfirmationLabel.Text = args.Result.Confirmation;
            Log($"copied {args.Result.Kind} {args.Result.Confirmation}");
            RefreshPresence();
        });
        _clipboard.Expired += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            ConfirmationLabel.Text = "Expired and cleared";
            Log($"expired {args.Kind}");
            RefreshPresence();
        });
        RefreshPresence();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshPresence();
    }

    async void OnCopyTextClicked(object? sender, EventArgs e)
    {
        try
        {
            await _clipboard.SetTextAsync(TextEntry.Text ?? string.Empty);
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnCopySensitiveClicked(object? sender, EventArgs e)
    {
        try
        {
            await _clipboard.SetSensitiveTextAsync(
                TextEntry.Text is { Length: > 0 } text ? text : "otp-123456",
                TimeSpan.FromMinutes(2));
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnCopyUriClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!Uri.TryCreate(UriEntry.Text, UriKind.Absolute, out var uri))
            {
                ConfirmationLabel.Text = "Enter an absolute URI.";
                return;
            }

            await _clipboard.SetUriAsync(uri);
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnCopyImageClicked(object? sender, EventArgs e)
    {
        try
        {
            await _clipboard.SetImageAsync(DemoPng.Bytes, "image/png");
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnCopyFileClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, "clipboardplus-demo.txt");
            await File.WriteAllTextAsync(path, "ClipboardPlus file demo");
            await _clipboard.SetFilesAsync([path]);
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnReadClicked(object? sender, EventArgs e)
    {
        try
        {
            var content = await _clipboard.GetContentAsync(new ClipboardReadOptions
            {
                IncludeText = true,
                IncludeUri = true,
                IncludeImage = true,
                IncludeFiles = true
            });
            Log($"read text={Truncate(content.Text)} uri={content.Uri} image={content.Image?.MimeType} files={content.Files.Count}");
            RefreshPresence();
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    async void OnClearClicked(object? sender, EventArgs e)
    {
        try
        {
            await _clipboard.ClearAsync();
            ConfirmationLabel.Text = "Cleared";
        }
        catch (Exception ex)
        {
            ConfirmationLabel.Text = ex.Message;
        }
    }

    void RefreshPresence()
    {
        var presence = _clipboard.GetPresence();
        var expires = _clipboard.ExpiresAt is { } value
            ? $" · expires {value.LocalDateTime:HH:mm:ss}"
            : "";
        PresenceLabel.Text =
            $"HasText={presence.HasText}  HasUri={presence.HasUri}  HasImage={presence.HasImage}  HasFiles={presence.HasFiles}{Environment.NewLine}" +
            $"Sensitive={presence.IsSensitive}{expires}";
    }

    void Log(string line)
    {
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss} {line}");
        if (_log.Count > 12)
            _log.RemoveAt(_log.Count - 1);
        LogLabel.Text = string.Join(Environment.NewLine, _log);
    }

    static string Format(ClipboardPresence presence) =>
        $"text={presence.HasText} uri={presence.HasUri} image={presence.HasImage} files={presence.HasFiles} sensitive={presence.IsSensitive}";

    static string Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? "—" : value.Length <= 24 ? value : value[..24] + "…";
}

static class DemoPng
{
    public static byte[] Bytes { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
