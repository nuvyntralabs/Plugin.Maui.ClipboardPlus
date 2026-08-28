namespace Plugin.Maui.ClipboardPlus;

sealed class ClipboardPlusImplementation : IClipboardPlus
{
    readonly ClipboardPlusOptions _options;
    readonly IClipboardPlatform _platform;
    readonly IClock _clock;
    readonly object _gate = new();
    SensitiveClipMarker? _owned;
    CancellationTokenSource? _expireCts;
    bool _monitoring;
    bool _disposed;
    ClipboardChangeSource _nextChangeSource = ClipboardChangeSource.Self;

    public ClipboardPlusImplementation(ClipboardPlusOptions options, IClipboardPlatform platform, IClock clock)
    {
        _options = options;
        _platform = platform;
        _clock = clock;
    }

    public bool IsSupported => _platform.IsSupported;

    public bool HasText => GetPresence().HasText;

    public bool HasImage => GetPresence().HasImage;

    public bool HasUri => GetPresence().HasUri;

    public bool HasFiles => GetPresence().HasFiles;

    public bool HasSensitiveContent => GetPresence().IsSensitive;

    public DateTimeOffset? ExpiresAt
    {
        get
        {
            lock (_gate)
                return _owned?.ExpiresAt;
        }
    }

    public event EventHandler<ClipboardChangedEventArgs>? ContentChanged;

    public event EventHandler<ClipboardCopiedEventArgs>? Copied;

    public event EventHandler<ClipboardExpiredEventArgs>? Expired;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_options.MonitorClipboard || _monitoring)
            return;

        _platform.StartMonitoring(OnPlatformChanged);
        _monitoring = true;
    }

    public ClipboardPresence GetPresence()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _platform.GetPresence();
    }

    public Task<ClipboardSetResult> SetTextAsync(string text, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return WriteAsync(
            ClipboardContentKind.Text,
            options,
            isSensitive: false,
            preview: text,
            (write, marker, token) => _platform.SetTextAsync(text, write, marker, token),
            cancellationToken);
    }

    public Task<ClipboardSetResult> SetSensitiveTextAsync(string text, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return SetTextAsync(
            text,
            new ClipboardWriteOptions { IsSensitive = true, Expiration = expiration },
            cancellationToken);
    }

    public Task<ClipboardSetResult> SetUriAsync(Uri uri, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            throw new ArgumentException("The URI must be absolute.", nameof(uri));

        return WriteAsync(
            ClipboardContentKind.Uri,
            options,
            isSensitive: false,
            preview: uri.ToString(),
            (write, marker, token) => _platform.SetUriAsync(uri, write, marker, token),
            cancellationToken);
    }

    public Task<ClipboardSetResult> SetImageAsync(byte[] image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length == 0)
            throw new ArgumentException("Image data is empty.", nameof(image));
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        return WriteAsync(
            ClipboardContentKind.Image,
            options,
            isSensitive: false,
            preview: mimeType,
            (write, marker, token) => _platform.SetImageAsync(image, mimeType, write, marker, token),
            cancellationToken);
    }

    public async Task<ClipboardSetResult> SetImageAsync(Stream image, string mimeType = "image/png", ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return await SetImageAsync(buffer.ToArray(), mimeType, options, cancellationToken).ConfigureAwait(false);
    }

    public Task<ClipboardSetResult> SetFilesAsync(IEnumerable<string> filePaths, ClipboardWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        var paths = filePaths.Where(static path => !string.IsNullOrWhiteSpace(path)).Select(static path => path.Trim()).ToArray();
        if (paths.Length == 0)
            throw new ArgumentException("At least one file path is required.", nameof(filePaths));

        return WriteAsync(
            ClipboardContentKind.Files,
            options,
            isSensitive: false,
            preview: paths.Length == 1 ? Path.GetFileName(paths[0]) : $"{paths.Length} files",
            (write, marker, token) => _platform.SetFilesAsync(paths, write, marker, token),
            cancellationToken);
    }

    public Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _platform.GetTextAsync(cancellationToken);
    }

    public Task<Uri?> GetUriAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _platform.GetUriAsync(cancellationToken);
    }

    public Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _platform.GetImageAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _platform.GetFilesAsync(cancellationToken);
    }

    public async Task<ClipboardContent> GetContentAsync(ClipboardReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        options ??= new ClipboardReadOptions();

        var presence = _platform.GetPresence();
        return new ClipboardContent
        {
            Presence = presence,
            Text = options.IncludeText ? await _platform.GetTextAsync(cancellationToken).ConfigureAwait(false) : null,
            Uri = options.IncludeUri ? await _platform.GetUriAsync(cancellationToken).ConfigureAwait(false) : null,
            Image = options.IncludeImage ? await _platform.GetImageAsync(cancellationToken).ConfigureAwait(false) : null,
            Files = options.IncludeFiles ? await _platform.GetFilesAsync(cancellationToken).ConfigureAwait(false) : [],
            ExpiresAt = ExpiresAt
        };
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _nextChangeSource = ClipboardChangeSource.Cleared;
        ForgetOwned();
        await _platform.ClearAsync(cancellationToken).ConfigureAwait(false);
        if (!_monitoring)
            RaiseChanged(ClipboardChangeSource.Cleared);
    }

    public Task ClearSensitiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SensitiveClipMarker? owned;
        lock (_gate)
            owned = _owned;

        if (owned is not { IsSensitive: true } || !_platform.IsCurrentClip(owned))
            return Task.CompletedTask;

        return ClearAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ForgetOwned();
        if (_monitoring)
        {
            _platform.StopMonitoring();
            _monitoring = false;
        }
    }

    internal Task ProcessDueExpirationsAsync() => ExpireOwnedIfDueAsync();

    async Task<ClipboardSetResult> WriteAsync(
        ClipboardContentKind kind,
        ClipboardWriteOptions? options,
        bool isSensitive,
        string? preview,
        Func<ClipboardWriteOptions, SensitiveClipMarker, CancellationToken, Task> write,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var writeOptions = Normalize(options, isSensitive);
        var expiresAt = ToExpiresAt(writeOptions.Expiration);
        var marker = SensitiveClipMarker.Create(kind, writeOptions.IsSensitive, expiresAt);
        _nextChangeSource = ClipboardChangeSource.Self;
        await write(writeOptions, marker, cancellationToken).ConfigureAwait(false);
        TrackOwned(marker);

        var result = new ClipboardSetResult
        {
            Succeeded = true,
            Kind = kind,
            IsSensitive = writeOptions.IsSensitive,
            ExpiresAt = expiresAt,
            Preview = writeOptions.IsSensitive ? null : TrimPreview(preview),
            Confirmation = FormatConfirmation(writeOptions.IsSensitive, expiresAt)
        };

        if (_options.RaiseCopiedEvent)
            Copied?.Invoke(this, new ClipboardCopiedEventArgs { Result = result });

        if (!_monitoring)
            RaiseChanged(ClipboardChangeSource.Self);

        return result;
    }

    ClipboardWriteOptions Normalize(ClipboardWriteOptions? options, bool isSensitive)
    {
        options ??= new ClipboardWriteOptions();
        var sensitive = isSensitive || options.IsSensitive;
        var expiration = options.Expiration;
        if (sensitive && expiration is null)
            expiration = _options.DefaultSensitiveExpiration;
        if (expiration is { } value && value < TimeSpan.Zero)
            expiration = TimeSpan.Zero;

        return new ClipboardWriteOptions
        {
            IsSensitive = sensitive,
            Expiration = expiration,
            Label = options.Label,
            LocalOnly = sensitive || options.LocalOnly
        };
    }

    DateTimeOffset? ToExpiresAt(TimeSpan? expiration) =>
        expiration is { } value ? _clock.UtcNow + value : null;

    void TrackOwned(SensitiveClipMarker marker)
    {
        CancellationTokenSource? previous;
        CancellationTokenSource? next = null;
        lock (_gate)
        {
            previous = _expireCts;
            _owned = marker;
            if (marker.ExpiresAt is not null)
            {
                next = new CancellationTokenSource();
                _expireCts = next;
            }
            else
            {
                _expireCts = null;
            }
        }

        previous?.Cancel();
        previous?.Dispose();
        if (next is null || marker.ExpiresAt is not { } expiresAt)
            return;

        var delay = expiresAt - _clock.UtcNow;
        _ = ExpireAfterAsync(delay, next.Token);
    }

    void ForgetOwned()
    {
        CancellationTokenSource? previous;
        lock (_gate)
        {
            previous = _expireCts;
            _expireCts = null;
            _owned = null;
        }

        previous?.Cancel();
        previous?.Dispose();
    }

    async Task ExpireAfterAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await ExpireOwnedIfDueAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task ExpireOwnedIfDueAsync()
    {
        SensitiveClipMarker? owned;
        lock (_gate)
            owned = _owned;

        if (owned?.ExpiresAt is not { } expiresAt || expiresAt > _clock.UtcNow)
            return;

        if (!_platform.IsCurrentClip(owned))
        {
            ForgetOwned();
            return;
        }

        _nextChangeSource = ClipboardChangeSource.Cleared;
        ForgetOwned();
        await _platform.ClearAsync(CancellationToken.None).ConfigureAwait(false);
        Expired?.Invoke(this, new ClipboardExpiredEventArgs { Kind = owned.Kind, ExpiresAt = expiresAt });
        if (!_monitoring)
            RaiseChanged(ClipboardChangeSource.Cleared);
    }

    void OnPlatformChanged()
    {
        var source = _nextChangeSource;
        _nextChangeSource = ClipboardChangeSource.External;

        SensitiveClipMarker? owned;
        lock (_gate)
            owned = _owned;

        if (source == ClipboardChangeSource.External
            && owned is not null
            && !_platform.IsCurrentClip(owned))
        {
            ForgetOwned();
        }

        RaiseChanged(source);
    }

    void RaiseChanged(ClipboardChangeSource source) =>
        ContentChanged?.Invoke(this, new ClipboardChangedEventArgs
        {
            Presence = _platform.GetPresence(),
            Source = source
        });

    string? TrimPreview(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var max = Math.Max(8, _options.PreviewMaxLength);
        return value.Length <= max ? value : value[..max] + "…";
    }

    string FormatConfirmation(bool isSensitive, DateTimeOffset? expiresAt)
    {
        if (expiresAt is not { } expires)
            return isSensitive ? "Copied (sensitive)" : "Copied";

        var remaining = expires - _clock.UtcNow;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        return isSensitive
            ? $"Copied (sensitive, expires in {FormatDuration(remaining)})"
            : $"Copied (expires in {FormatDuration(remaining)})";
    }

    static string FormatDuration(TimeSpan value)
    {
        if (value.TotalSeconds < 1)
            return "a moment";
        if (value.TotalMinutes < 1)
            return $"{Math.Ceiling(value.TotalSeconds)} seconds";
        if (value.TotalHours < 1)
        {
            var minutes = Math.Ceiling(value.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        var hours = Math.Ceiling(value.TotalHours);
        return hours == 1 ? "1 hour" : $"{hours} hours";
    }
}
