namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Result of a successful clipboard write, used as a copy confirmation.
/// </summary>
public sealed class ClipboardSetResult
{
    /// <summary>
    /// Always <c>true</c> when the method returns (failures throw).
    /// </summary>
    public bool Succeeded { get; init; } = true;

    /// <summary>
    /// Kind that was written.
    /// </summary>
    public ClipboardContentKind Kind { get; init; }

    /// <summary>
    /// Whether the clip was marked sensitive.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// When the clip will be cleared, if an expiration was set.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Short preview of non-sensitive text. Sensitive writes are always redacted.
    /// </summary>
    public string? Preview { get; init; }

    /// <summary>
    /// Human-readable confirmation, for example <c>Copied</c> or
    /// <c>Copied (expires in 2 minutes)</c>.
    /// </summary>
    public required string Confirmation { get; init; }
}
