#if IOS
using Foundation;
using UIKit;

namespace Plugin.Maui.ClipboardPlus;

sealed class IosClipboardPlatform : IClipboardPlatform
{
    const string MarkerUti = "com.mauiessentials.clipboardplus.marker";
    const string SensitiveUti = "com.mauiessentials.clipboardplus.sensitive";
    const string KindUti = "com.mauiessentials.clipboardplus.kind";
    const string PlainTextUti = "public.utf8-plain-text";
    const string UrlUti = "public.url";
    const string FileUrlUti = "public.file-url";
    const string PngUti = "public.png";
    const string JpegUti = "public.jpeg";

    NSObject? _observer;

    public bool IsSupported => true;

    public ClipboardPresence GetPresence() =>
        Run(() =>
        {
            var pasteboard = UIPasteboard.General;
            return new ClipboardPresence
            {
                HasText = pasteboard.HasStrings,
                HasUri = pasteboard.HasUrls,
                HasImage = pasteboard.HasImages,
                HasFiles = pasteboard.Contains([FileUrlUti]) && !pasteboard.HasImages,
                IsSensitive = HasMarkerValue(SensitiveUti, "1")
            };
        });

    public Task SetTextAsync(string text, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var item = NewItem(marker, options);
            item[new NSString(PlainTextUti)] = new NSString(text);
            SetItems([item], options, marker);
        }, cancellationToken);

    public Task SetUriAsync(Uri uri, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var item = NewItem(marker, options);
            if (NSUrl.FromString(uri.AbsoluteUri) is { } nsUrl)
                item[new NSString(UrlUti)] = nsUrl;
            item[new NSString(PlainTextUti)] = new NSString(uri.AbsoluteUri);
            SetItems([item], options, marker);
        }, cancellationToken);

    public Task SetImageAsync(byte[] image, string mimeType, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var item = NewItem(marker, options);
            var uti = mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? JpegUti : PngUti;
            item[new NSString(uti)] = NSData.FromArray(image);
            SetItems([item], options, marker);
        }, cancellationToken);

    public Task SetFilesAsync(IReadOnlyList<string> filePaths, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var items = filePaths.Select(path =>
            {
                var item = NewItem(marker, options);
                item[new NSString(FileUrlUti)] = NSUrl.FromFilename(path);
                return item;
            }).ToArray();
            SetItems(items, options, marker);
        }, cancellationToken);

    public Task<string?> GetTextAsync(CancellationToken cancellationToken) =>
        RunAsync(() => UIPasteboard.General.String, cancellationToken);

    public Task<Uri?> GetUriAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            if (UIPasteboard.General.Url is { } url && Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var parsed))
                return parsed;
            if (Uri.TryCreate(UIPasteboard.General.String, UriKind.Absolute, out var fromText)
                && fromText.Scheme is "http" or "https" or "mailto" or "tel")
                return fromText;
            return null;
        }, cancellationToken);

    public Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var image = UIPasteboard.General.Image;
            if (image is null)
                return null;

            var data = image.AsPNG();
            if (data is null)
                return null;

            return new ClipboardImage { Data = data.ToArray(), MimeType = "image/png" };
        }, cancellationToken);

    public Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var files = new List<ClipboardFile>();
            foreach (var item in UIPasteboard.General.Items)
            {
                if (item[FileUrlUti] is NSUrl fileUrl)
                {
                    files.Add(new ClipboardFile
                    {
                        Location = fileUrl.Path ?? fileUrl.AbsoluteString ?? string.Empty,
                        FileName = Path.GetFileName(fileUrl.Path)
                    });
                }
            }

            return (IReadOnlyList<ClipboardFile>)files;
        }, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            UIPasteboard.General.Items = [];
        }, cancellationToken);

    public bool IsCurrentClip(SensitiveClipMarker marker) =>
        Run(() => HasMarkerValue(MarkerUti, marker.Id));

    public void StartMonitoring(Action onChanged)
    {
        StopMonitoring();
        _observer = NSNotificationCenter.DefaultCenter.AddObserver(
            UIPasteboard.ChangedNotification,
            _ => onChanged());
    }

    public void StopMonitoring()
    {
        if (_observer is null)
            return;

        NSNotificationCenter.DefaultCenter.RemoveObserver(_observer);
        _observer.Dispose();
        _observer = null;
    }

    static NSMutableDictionary<NSString, NSObject> NewItem(SensitiveClipMarker marker, ClipboardWriteOptions options)
    {
        var item = new NSMutableDictionary<NSString, NSObject>
        {
            [new NSString(MarkerUti)] = new NSString(marker.Id),
            [new NSString(KindUti)] = new NSString(marker.Kind.ToString())
        };
        if (options.IsSensitive)
            item[new NSString(SensitiveUti)] = new NSString("1");
        return item;
    }

    static void SetItems(NSMutableDictionary<NSString, NSObject>[] items, ClipboardWriteOptions options, SensitiveClipMarker marker)
    {
        var pasteOptions = new UIPasteboardOptions();
        if (options.LocalOnly)
            pasteOptions.LocalOnly = true;
        if (marker.ExpiresAt is { } expiresAt)
            pasteOptions.ExpirationDate = (NSDate)expiresAt.UtcDateTime;

        var dictionaries = items.Select(ToImmutable).ToArray();
        UIPasteboard.General.SetItems(dictionaries, pasteOptions);
    }

    static NSDictionary<NSString, NSObject> ToImmutable(NSMutableDictionary<NSString, NSObject> item)
    {
        var keys = item.Keys;
        var values = new NSObject[keys.Length];
        for (var i = 0; i < keys.Length; i++)
            values[i] = item[keys[i]];
        return new NSDictionary<NSString, NSObject>(keys, values);
    }

    static bool HasMarkerValue(string uti, string expected)
    {
        foreach (var item in UIPasteboard.General.Items)
        {
            if (item[uti] is NSString value && value.ToString() == expected)
                return true;
        }

        return false;
    }

    static T Run<T>(Func<T> action)
    {
        if (MainThread.IsMainThread)
            return action();
        return MainThread.InvokeOnMainThreadAsync(action).GetAwaiter().GetResult();
    }

    static Task RunAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (MainThread.IsMainThread)
        {
            action();
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(action);
    }

    static Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (MainThread.IsMainThread)
            return Task.FromResult(action());
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
#endif
