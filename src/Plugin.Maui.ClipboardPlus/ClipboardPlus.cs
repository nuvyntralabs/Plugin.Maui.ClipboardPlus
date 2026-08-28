namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Entry point for ClipboardPlus when dependency injection is not used.
/// </summary>
public static class ClipboardPlus
{
    static IClipboardPlus? _current;

    /// <summary>
    /// Gets the shared <see cref="IClipboardPlus"/> instance.
    /// </summary>
    public static IClipboardPlus Current => _current ??= Create(new ClipboardPlusOptions());

    /// <summary>
    /// Whether the clipboard currently has plain text.
    /// </summary>
    public static bool HasText => Current.HasText;

    /// <summary>
    /// Whether the clipboard currently has an image.
    /// </summary>
    public static bool HasImage => Current.HasImage;

    /// <summary>
    /// Whether the clipboard currently has a URI.
    /// </summary>
    public static bool HasUri => Current.HasUri;

    /// <summary>
    /// Whether the clipboard currently has one or more files.
    /// </summary>
    public static bool HasFiles => Current.HasFiles;

    /// <summary>
    /// Raised when the OS clipboard changes.
    /// </summary>
    public static event EventHandler<ClipboardChangedEventArgs>? ContentChanged
    {
        add => Current.ContentChanged += value;
        remove => Current.ContentChanged -= value;
    }

    /// <summary>
    /// Raised after a successful write from this process.
    /// </summary>
    public static event EventHandler<ClipboardCopiedEventArgs>? Copied
    {
        add => Current.Copied += value;
        remove => Current.Copied -= value;
    }

    /// <summary>
    /// Raised after an expired clip this process still owns is cleared.
    /// </summary>
    public static event EventHandler<ClipboardExpiredEventArgs>? Expired
    {
        add => Current.Expired += value;
        remove => Current.Expired -= value;
    }

    /// <summary>
    /// Copies plain text.
    /// </summary>
    /// <example>
    /// <code>
    /// await ClipboardPlus.SetTextAsync("hello");
    /// </code>
    /// </example>
    public static Task<ClipboardSetResult> SetTextAsync(string text, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.SetTextAsync(text, options, cancellationToken);

    /// <summary>
    /// Copies a secret and clears it when <paramref name="expiration"/> elapses.
    /// </summary>
    /// <example>
    /// <code>
    /// await ClipboardPlus.SetSensitiveTextAsync(token, expiration: TimeSpan.FromMinutes(2));
    /// </code>
    /// </example>
    public static Task<ClipboardSetResult> SetSensitiveTextAsync(string text, TimeSpan? expiration = null, CancellationToken cancellationToken = default) =>
        Current.SetSensitiveTextAsync(text, expiration, cancellationToken);

    /// <summary>
    /// Copies a URI.
    /// </summary>
    public static Task<ClipboardSetResult> SetUriAsync(Uri uri, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.SetUriAsync(uri, options, cancellationToken);

    /// <summary>
    /// Copies an image.
    /// </summary>
    public static Task<ClipboardSetResult> SetImageAsync(byte[] image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.SetImageAsync(image, mimeType, options, cancellationToken);

    /// <summary>
    /// Copies an image from a stream.
    /// </summary>
    public static Task<ClipboardSetResult> SetImageAsync(Stream image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.SetImageAsync(image, mimeType, options, cancellationToken);

    /// <summary>
    /// Copies one or more local file paths.
    /// </summary>
    public static Task<ClipboardSetResult> SetFilesAsync(IEnumerable<string> filePaths, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.SetFilesAsync(filePaths, options, cancellationToken);

    /// <summary>
    /// Reads plain text.
    /// </summary>
    public static Task<string?> GetTextAsync(CancellationToken cancellationToken = default) =>
        Current.GetTextAsync(cancellationToken);

    /// <summary>
    /// Reads a URI.
    /// </summary>
    public static Task<Uri?> GetUriAsync(CancellationToken cancellationToken = default) =>
        Current.GetUriAsync(cancellationToken);

    /// <summary>
    /// Reads an image.
    /// </summary>
    public static Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken = default) =>
        Current.GetImageAsync(cancellationToken);

    /// <summary>
    /// Reads file references.
    /// </summary>
    public static Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken = default) =>
        Current.GetFilesAsync(cancellationToken);

    /// <summary>
    /// Reads a snapshot of clipboard contents.
    /// </summary>
    public static Task<ClipboardContent> GetContentAsync(ClipboardReadOptions? options = null, CancellationToken cancellationToken = default) =>
        Current.GetContentAsync(options, cancellationToken);

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    public static Task ClearAsync(CancellationToken cancellationToken = default) =>
        Current.ClearAsync(cancellationToken);

    /// <summary>
    /// Clears the clipboard only if this process still owns a sensitive clip.
    /// </summary>
    public static Task ClearSensitiveAsync(CancellationToken cancellationToken = default) =>
        Current.ClearSensitiveAsync(cancellationToken);

    /// <summary>
    /// Presence flags without reading clipboard payloads.
    /// </summary>
    public static ClipboardPresence GetPresence() => Current.GetPresence();

    /// <summary>
    /// Creates a clipboard client for the current platform.
    /// </summary>
    public static IClipboardPlus Create(ClipboardPlusOptions? options = null)
    {
        options ??= new ClipboardPlusOptions();
        return new ClipboardPlusImplementation(options, CreatePlatform(), SystemClock.Instance);
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(IClipboardPlus implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static ClipboardPlusImplementation Create(
        ClipboardPlusOptions options,
        IClipboardPlatform platform,
        IClock clock) =>
        new(options, platform, clock);

    static IClipboardPlatform CreatePlatform()
    {
#if ANDROID
        return new AndroidClipboardPlatform();
#elif IOS
        return new IosClipboardPlatform();
#else
        return new InMemoryClipboardPlatform();
#endif
    }
}
