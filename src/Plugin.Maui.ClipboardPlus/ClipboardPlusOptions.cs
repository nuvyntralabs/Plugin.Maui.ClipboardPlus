namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Process-wide defaults for <see cref="IClipboardPlus"/>.
/// </summary>
public sealed class ClipboardPlusOptions
{
    /// <summary>
    /// Expiration used by <see cref="IClipboardPlus.SetSensitiveTextAsync"/> when the
    /// caller does not pass one. Default is two minutes.
    /// </summary>
    public TimeSpan DefaultSensitiveExpiration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// When <c>true</c>, a sensitive clip this process still owns is cleared when the
    /// app backgrounds. Default is <c>true</c>.
    /// </summary>
    public bool ClearSensitiveOnAppBackground { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, <see cref="IClipboardPlus.ContentChanged"/> is wired to the
    /// OS clipboard listener. Default is <c>true</c>.
    /// </summary>
    public bool MonitorClipboard { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, successful writes raise <see cref="IClipboardPlus.Copied"/>.
    /// Default is <c>true</c>.
    /// </summary>
    public bool RaiseCopiedEvent { get; set; } = true;

    /// <summary>
    /// Maximum characters kept in a non-sensitive copy preview. Default is 80.
    /// </summary>
    public int PreviewMaxLength { get; set; } = 80;
}
