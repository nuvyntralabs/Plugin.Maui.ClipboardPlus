namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Who last changed the clipboard from this process's point of view.
/// </summary>
public enum ClipboardChangeSource
{
    /// <summary>
    /// This process wrote the current clip.
    /// </summary>
    Self,

    /// <summary>
    /// Another app or the user replaced the clipboard.
    /// </summary>
    External,

    /// <summary>
    /// This process cleared the clipboard (explicit clear or expiration).
    /// </summary>
    Cleared
}
