namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Raised after this process successfully writes to the clipboard.
/// </summary>
public sealed class ClipboardCopiedEventArgs : EventArgs
{
    /// <summary>
    /// The write result, including confirmation text and a redacted preview.
    /// </summary>
    public required ClipboardSetResult Result { get; init; }
}
