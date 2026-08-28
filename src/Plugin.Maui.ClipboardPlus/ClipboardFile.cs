namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// A file referenced by the clipboard.
/// </summary>
public sealed class ClipboardFile
{
    /// <summary>
    /// File path or content URI the OS exposed.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// File name when the platform provides one.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// MIME type when the platform provides one.
    /// </summary>
    public string? MimeType { get; init; }
}
