namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Raised after a sensitive or time-limited clip is cleared because it expired.
/// </summary>
public sealed class ClipboardExpiredEventArgs : EventArgs
{
    /// <summary>
    /// Kind that was cleared.
    /// </summary>
    public ClipboardContentKind Kind { get; init; }

    /// <summary>
    /// When the clip was scheduled to expire.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
