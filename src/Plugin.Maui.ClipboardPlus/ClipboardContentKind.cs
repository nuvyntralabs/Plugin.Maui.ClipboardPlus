namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Kinds of data that can be present on the clipboard.
/// </summary>
[Flags]
public enum ClipboardContentKind
{
    /// <summary>
    /// The clipboard is empty or the kind is unknown.
    /// </summary>
    None = 0,

    /// <summary>
    /// Plain text.
    /// </summary>
    Text = 1,

    /// <summary>
    /// An HTTP(S) or other absolute URI.
    /// </summary>
    Uri = 2,

    /// <summary>
    /// An image.
    /// </summary>
    Image = 4,

    /// <summary>
    /// One or more local or content-URI files.
    /// </summary>
    Files = 8
}
