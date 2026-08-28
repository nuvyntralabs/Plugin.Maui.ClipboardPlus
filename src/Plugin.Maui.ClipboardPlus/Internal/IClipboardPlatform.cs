namespace Plugin.Maui.ClipboardPlus;

interface IClipboardPlatform
{
    bool IsSupported { get; }

    ClipboardPresence GetPresence();

    Task SetTextAsync(string text, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken);

    Task SetUriAsync(Uri uri, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken);

    Task SetImageAsync(byte[] image, string mimeType, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken);

    Task SetFilesAsync(IReadOnlyList<string> filePaths, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken);

    Task<string?> GetTextAsync(CancellationToken cancellationToken);

    Task<Uri?> GetUriAsync(CancellationToken cancellationToken);

    Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);

    bool IsCurrentClip(SensitiveClipMarker marker);

    void StartMonitoring(Action onChanged);

    void StopMonitoring();
}
