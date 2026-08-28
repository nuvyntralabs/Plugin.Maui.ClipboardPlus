namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Raised when the OS clipboard contents change.
/// </summary>
public sealed class ClipboardChangedEventArgs : EventArgs
{
    /// <summary>
    /// Presence after the change (no payload read).
    /// </summary>
    public required ClipboardPresence Presence { get; init; }

    /// <summary>
    /// Whether this process, another app, or a clear caused the change.
    /// </summary>
    public ClipboardChangeSource Source { get; init; }
}
