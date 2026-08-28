namespace Plugin.Maui.ClipboardPlus;

sealed class InMemoryClipboardPlatform : IClipboardPlatform
{
    readonly object _gate = new();
    string? _text;
    Uri? _uri;
    ClipboardImage? _image;
    IReadOnlyList<ClipboardFile> _files = [];
    SensitiveClipMarker? _marker;
    Action? _changed;

    public bool IsSupported => true;

    public ClipboardPresence GetPresence()
    {
        lock (_gate)
        {
            return new ClipboardPresence
            {
                HasText = _text is not null,
                HasUri = _uri is not null,
                HasImage = _image is not null,
                HasFiles = _files.Count > 0,
                IsSensitive = _marker?.IsSensitive == true
            };
        }
    }

    public Task SetTextAsync(string text, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken)
    {
        Replace(() =>
        {
            _text = text;
            _uri = null;
            _image = null;
            _files = [];
            _marker = marker;
        });
        return Task.CompletedTask;
    }

    public Task SetUriAsync(Uri uri, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken)
    {
        Replace(() =>
        {
            _text = uri.ToString();
            _uri = uri;
            _image = null;
            _files = [];
            _marker = marker;
        });
        return Task.CompletedTask;
    }

    public Task SetImageAsync(byte[] image, string mimeType, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken)
    {
        Replace(() =>
        {
            _text = null;
            _uri = null;
            _image = new ClipboardImage { Data = image, MimeType = mimeType };
            _files = [];
            _marker = marker;
        });
        return Task.CompletedTask;
    }

    public Task SetFilesAsync(IReadOnlyList<string> filePaths, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken)
    {
        Replace(() =>
        {
            _text = null;
            _uri = null;
            _image = null;
            _files = filePaths
                .Select(path => new ClipboardFile
                {
                    Location = path,
                    FileName = Path.GetFileName(path)
                })
                .ToArray();
            _marker = marker;
        });
        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult(_text);
    }

    public Task<Uri?> GetUriAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult(_uri);
    }

    public Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult(_image);
    }

    public Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult(_files);
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        Replace(() =>
        {
            _text = null;
            _uri = null;
            _image = null;
            _files = [];
            _marker = null;
        });
        return Task.CompletedTask;
    }

    public bool IsCurrentClip(SensitiveClipMarker marker)
    {
        lock (_gate)
            return _marker is { } current && current.Id == marker.Id;
    }

    public void StartMonitoring(Action onChanged) => _changed = onChanged;

    public void StopMonitoring() => _changed = null;

    void Replace(Action mutate)
    {
        lock (_gate)
            mutate();
        _changed?.Invoke();
    }
}
