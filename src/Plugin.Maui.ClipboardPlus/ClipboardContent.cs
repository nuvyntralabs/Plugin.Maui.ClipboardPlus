namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// A snapshot of clipboard contents. Reading text, URI, image, or files may show
/// the iOS paste permission banner when the data came from another app.
/// </summary>
public sealed class ClipboardContent
{
    /// <summary>
    /// Presence flags (also available without reading via <see cref="IClipboardPlus.GetPresence"/>).
    /// </summary>
    public ClipboardPresence Presence { get; init; } = ClipboardPresence.Empty;

    /// <summary>
    /// Plain text, if requested and present.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// URI, if requested and present.
    /// </summary>
    public Uri? Uri { get; init; }

    /// <summary>
    /// Image, if requested and present.
    /// </summary>
    public ClipboardImage? Image { get; init; }

    /// <summary>
    /// Files, if requested and present.
    /// </summary>
    public IReadOnlyList<ClipboardFile> Files { get; init; } = [];

    /// <summary>
    /// When this process scheduled the current clip to expire, if still owned.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
