namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Clipboard for Android and iOS with text, URI, image, files, sensitive content,
/// automatic expiration, monitoring, and copy confirmation.
/// </summary>
public interface IClipboardPlus : IDisposable
{
    /// <summary>
    /// Always <c>true</c> on Android, iOS, and the in-memory <c>net10.0</c> surface.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Whether the clipboard currently has plain text. Does not read the payload.
    /// </summary>
    bool HasText { get; }

    /// <summary>
    /// Whether the clipboard currently has an image. Does not read the payload.
    /// </summary>
    bool HasImage { get; }

    /// <summary>
    /// Whether the clipboard currently has a URI. Does not read the payload.
    /// </summary>
    bool HasUri { get; }

    /// <summary>
    /// Whether the clipboard currently has one or more files. Does not read the payload.
    /// </summary>
    bool HasFiles { get; }

    /// <summary>
    /// Whether the current clip is marked sensitive.
    /// </summary>
    bool HasSensitiveContent { get; }

    /// <summary>
    /// When the clip this process still owns will expire, if any.
    /// </summary>
    DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Raised when the OS clipboard changes (this process or another app).
    /// </summary>
    event EventHandler<ClipboardChangedEventArgs>? ContentChanged;

    /// <summary>
    /// Raised after a successful write from this process (copy confirmation).
    /// </summary>
    event EventHandler<ClipboardCopiedEventArgs>? Copied;

    /// <summary>
    /// Raised after this process clears an expired clip it still owns.
    /// </summary>
    event EventHandler<ClipboardExpiredEventArgs>? Expired;

    /// <summary>
    /// Starts OS clipboard monitoring when <see cref="ClipboardPlusOptions.MonitorClipboard"/> is enabled.
    /// Safe to call more than once.
    /// </summary>
    void Start();

    /// <summary>
    /// Presence flags without reading clipboard payloads.
    /// </summary>
    ClipboardPresence GetPresence();

    /// <summary>
    /// Copies plain text.
    /// </summary>
    Task<ClipboardSetResult> SetTextAsync(string text, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a secret (OTP, token, password) as sensitive text and clears it when
    /// <paramref name="expiration"/> elapses. Defaults to
    /// <see cref="ClipboardPlusOptions.DefaultSensitiveExpiration"/>.
    /// </summary>
    Task<ClipboardSetResult> SetSensitiveTextAsync(string text, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a URI.
    /// </summary>
    Task<ClipboardSetResult> SetUriAsync(Uri uri, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an image.
    /// </summary>
    Task<ClipboardSetResult> SetImageAsync(byte[] image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an image from a stream. The stream is read fully and not disposed.
    /// </summary>
    Task<ClipboardSetResult> SetImageAsync(Stream image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies one or more local file paths.
    /// </summary>
    Task<ClipboardSetResult> SetFilesAsync(IEnumerable<string> filePaths, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads plain text. May trigger the iOS paste permission banner.
    /// </summary>
    Task<string?> GetTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a URI. May trigger the iOS paste permission banner.
    /// </summary>
    Task<Uri?> GetUriAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an image. May trigger the iOS paste permission banner.
    /// </summary>
    Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads file references. May trigger the iOS paste permission banner.
    /// </summary>
    Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a snapshot. Image and files are included only when
    /// <see cref="ClipboardReadOptions.IncludeImage"/> /
    /// <see cref="ClipboardReadOptions.IncludeFiles"/> are set.
    /// </summary>
    Task<ClipboardContent> GetContentAsync(ClipboardReadOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the clipboard only if this process still owns a sensitive clip.
    /// </summary>
    Task ClearSensitiveAsync(CancellationToken cancellationToken = default);
}
