namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// What the clipboard currently holds, without reading the payload.
/// On iOS 14+ this uses <c>HasStrings</c> / <c>HasURLs</c> / <c>HasImages</c> so it
/// does not trigger the paste permission banner.
/// </summary>
public sealed class ClipboardPresence
{
    /// <summary>
    /// An empty clipboard.
    /// </summary>
    public static ClipboardPresence Empty { get; } = new();

    /// <summary>
    /// Gets whether plain text is present.
    /// </summary>
    public bool HasText { get; init; }

    /// <summary>
    /// Gets whether a URI is present.
    /// </summary>
    public bool HasUri { get; init; }

    /// <summary>
    /// Gets whether an image is present.
    /// </summary>
    public bool HasImage { get; init; }

    /// <summary>
    /// Gets whether one or more files are present.
    /// </summary>
    public bool HasFiles { get; init; }

    /// <summary>
    /// Gets whether the current clip was marked sensitive by this process or the OS.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Combined kind flags.
    /// </summary>
    public ClipboardContentKind Kind
    {
        get
        {
            var kind = ClipboardContentKind.None;
            if (HasText)
                kind |= ClipboardContentKind.Text;
            if (HasUri)
                kind |= ClipboardContentKind.Uri;
            if (HasImage)
                kind |= ClipboardContentKind.Image;
            if (HasFiles)
                kind |= ClipboardContentKind.Files;
            return kind;
        }
    }

    /// <summary>
    /// Gets whether any supported content is present.
    /// </summary>
    public bool HasContent => Kind != ClipboardContentKind.None;
}
