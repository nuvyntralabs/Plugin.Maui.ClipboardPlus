namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// An image read from the clipboard.
/// </summary>
public sealed class ClipboardImage
{
    /// <summary>
    /// Encoded image bytes (PNG, JPEG, or the platform's native encoding).
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// MIME type, for example <c>image/png</c>.
    /// </summary>
    public required string MimeType { get; init; }
}
