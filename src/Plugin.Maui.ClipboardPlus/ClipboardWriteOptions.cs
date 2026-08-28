namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Options applied when writing to the clipboard.
/// </summary>
public sealed class ClipboardWriteOptions
{
    /// <summary>
    /// Marks the clip as sensitive. Android 13+ hides it from the clipboard overlay;
    /// iOS keeps it off Universal Clipboard when <see cref="LocalOnly"/> is also set
    /// (the default for sensitive writes).
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// How long the clip should remain. Sensitive writes without an expiration use
    /// <see cref="ClipboardPlusOptions.DefaultSensitiveExpiration"/>.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Optional label shown by the OS clipboard UI (Android).
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// When <c>true</c>, iOS does not sync the clip through Universal Clipboard / Handoff.
    /// Sensitive writes default this to <c>true</c>.
    /// </summary>
    public bool LocalOnly { get; set; }
}
