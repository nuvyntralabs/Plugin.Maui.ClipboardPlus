namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Controls which payloads <see cref="IClipboardPlus.GetContentAsync"/> reads.
/// Presence flags are always populated.
/// </summary>
public sealed class ClipboardReadOptions
{
    /// <summary>
    /// Read plain text. Default is <c>true</c>.
    /// </summary>
    public bool IncludeText { get; set; } = true;

    /// <summary>
    /// Read a URI. Default is <c>true</c>.
    /// </summary>
    public bool IncludeUri { get; set; } = true;

    /// <summary>
    /// Read image bytes. Default is <c>false</c> (can be large).
    /// </summary>
    public bool IncludeImage { get; set; }

    /// <summary>
    /// Read file references. Default is <c>false</c>.
    /// </summary>
    public bool IncludeFiles { get; set; }
}
