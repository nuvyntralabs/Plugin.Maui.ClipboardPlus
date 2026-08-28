#if ANDROID
using Android.Content;
using Android.OS;
using AndroidUri = Android.Net.Uri;
using ClipData = Android.Content.ClipData;
using JavaFile = Java.IO.File;

namespace Plugin.Maui.ClipboardPlus;

sealed class AndroidClipboardPlatform : IClipboardPlatform
{
    const string ExtraMarker = "me.clipboardplus.id";
    const string ExtraKind = "me.clipboardplus.kind";
    const string ExtraSensitive = "me.clipboardplus.sensitive";

    readonly ClipListener _listener = new();
    bool _listening;

    public bool IsSupported => true;

    public ClipboardPresence GetPresence() =>
        Run(() =>
        {
            var clipboard = Manager();
            if (clipboard is null || !clipboard.HasPrimaryClip)
                return ClipboardPresence.Empty;

            var description = clipboard.PrimaryClipDescription;
            if (description is null)
                return ClipboardPresence.Empty;

            var kindHint = ReadKind(description);
            var hasText = description.HasMimeType(ClipDescription.MimetypeTextPlain)
                || description.HasMimeType(ClipDescription.MimetypeTextHtml);
            var hasUri = description.HasMimeType(ClipDescription.MimetypeTextUrilist)
                || kindHint == ClipboardContentKind.Uri;
            var hasImage = HasImageMime(description) || kindHint == ClipboardContentKind.Image;
            var hasFiles = kindHint == ClipboardContentKind.Files
                || (!hasImage && !hasUri && HasFileMime(description));

            return new ClipboardPresence
            {
                HasText = hasText,
                HasUri = hasUri,
                HasImage = hasImage,
                HasFiles = hasFiles,
                IsSensitive = ReadSensitive(description)
            };
        });

    public Task SetTextAsync(string text, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var clip = ClipData.NewPlainText(marker.ToAndroidLabel(options.Label), text)
                ?? throw new InvalidOperationException("Could not create text clip data.");
            ApplyMetadata(clip, options, marker);
            SetClip(clip);
        }, cancellationToken);

    public Task SetUriAsync(Uri uri, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var androidUri = AndroidUri.Parse(uri.ToString())
                ?? throw new InvalidOperationException("The URI could not be converted for Android.");
            var clip = ClipData.NewRawUri(marker.ToAndroidLabel(options.Label), androidUri)
                ?? throw new InvalidOperationException("Could not create URI clip data.");
            ApplyMetadata(clip, options, marker);
            SetClip(clip);
        }, cancellationToken);

    public Task SetImageAsync(byte[] image, string mimeType, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var file = WriteCacheFile(marker.Id, ExtensionFor(mimeType), image);
            var uri = ToShareableUri(file);
            var clip = new ClipData(marker.ToAndroidLabel(options.Label), [mimeType], new ClipData.Item(uri));
            ApplyMetadata(clip, options, marker);
            SetClip(clip);
        }, cancellationToken);

    public Task SetFilesAsync(IReadOnlyList<string> filePaths, ClipboardWriteOptions options, SensitiveClipMarker marker, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var context = AppContext();
            var first = ToShareableUri(new JavaFile(filePaths[0]));
            var mime = context.ContentResolver?.GetType(first) ?? "*/*";
            var clip = new ClipData(marker.ToAndroidLabel(options.Label), [mime], new ClipData.Item(first));
            for (var i = 1; i < filePaths.Count; i++)
                clip.AddItem(new ClipData.Item(ToShareableUri(new JavaFile(filePaths[i]))));
            ApplyMetadata(clip, options, marker);
            SetClip(clip);
        }, cancellationToken);

    public Task<string?> GetTextAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var item = FirstItem();
            return item?.CoerceToText(AppContext())?.ToString();
        }, cancellationToken);

    public Task<Uri?> GetUriAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var item = FirstItem();
            if (item?.Uri is { } androidUri && TryCreateUri(androidUri.ToString(), out var fromItem))
                return fromItem;
            if (TryCreateUri(item?.Text?.ToString(), out var fromText) && IsWebOrAbsolute(fromText))
                return fromText;
            return null;
        }, cancellationToken);

    public Task<ClipboardImage?> GetImageAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var clipboard = Manager();
            var clip = clipboard?.PrimaryClip;
            if (clip is null)
                return null;

            for (var i = 0; i < clip.ItemCount; i++)
            {
                var uri = clip.GetItemAt(i)?.Uri;
                if (uri is null)
                    continue;

                var mime = AppContext().ContentResolver?.GetType(uri) ?? "image/*";
                if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = AppContext().ContentResolver?.OpenInputStream(uri);
                if (stream is null)
                    continue;

                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                return new ClipboardImage { Data = buffer.ToArray(), MimeType = mime };
            }

            return null;
        }, cancellationToken);

    public Task<IReadOnlyList<ClipboardFile>> GetFilesAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var clip = Manager()?.PrimaryClip;
            if (clip is null)
                return (IReadOnlyList<ClipboardFile>)[];

            var files = new List<ClipboardFile>();
            for (var i = 0; i < clip.ItemCount; i++)
            {
                var uri = clip.GetItemAt(i)?.Uri;
                if (uri is null)
                    continue;

                var value = uri.ToString();
                if (string.IsNullOrWhiteSpace(value) || IsWeb(value))
                    continue;

                files.Add(new ClipboardFile
                {
                    Location = value,
                    FileName = uri.LastPathSegment,
                    MimeType = AppContext().ContentResolver?.GetType(uri)
                });
            }

            return files;
        }, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var clipboard = Manager();
            if (clipboard is null)
                return;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                clipboard.ClearPrimaryClip();
            else
                clipboard.PrimaryClip = ClipData.NewPlainText("", "");
        }, cancellationToken);

    public bool IsCurrentClip(SensitiveClipMarker marker) =>
        Run(() =>
        {
            var description = Manager()?.PrimaryClipDescription;
            if (description is null)
                return false;

            if (ReadMarkerId(description) == marker.Id)
                return true;

            return SensitiveClipMarker.TryGetIdFromAndroidLabel(description.Label, out var id)
                && id == marker.Id;
        });

    public void StartMonitoring(Action onChanged)
    {
        _listener.Changed = onChanged;
        if (_listening)
            return;

        Manager()?.AddPrimaryClipChangedListener(_listener);
        _listening = true;
    }

    public void StopMonitoring()
    {
        if (!_listening)
            return;

        Manager()?.RemovePrimaryClipChangedListener(_listener);
        _listener.Changed = null;
        _listening = false;
    }

    static void ApplyMetadata(ClipData clip, ClipboardWriteOptions options, SensitiveClipMarker marker)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
            return;

        var description = clip.Description
            ?? throw new InvalidOperationException("Clip description is not available.");
        var extras = description.Extras ?? new PersistableBundle();
        extras.PutString(ExtraMarker, marker.Id);
        extras.PutString(ExtraKind, marker.Kind.ToString());
        extras.PutBoolean(ExtraSensitive, options.IsSensitive);
        if (options.IsSensitive && OperatingSystem.IsAndroidVersionAtLeast(33))
            extras.PutBoolean(ClipDescription.ExtraIsSensitive, true);
        description.Extras = extras;
    }

    static string? ReadMarkerId(ClipDescription description)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(24) && description.Extras is { } extras)
            return extras.GetString(ExtraMarker);
        return null;
    }

    static ClipboardContentKind? ReadKind(ClipDescription description)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24) || description.Extras is not { } extras)
            return null;

        return Enum.TryParse<ClipboardContentKind>(extras.GetString(ExtraKind), out var kind)
            ? kind
            : null;
    }

    static bool ReadSensitive(ClipDescription description)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24) || description.Extras is not { } extras)
            return SensitiveClipMarker.TryGetIdFromAndroidLabel(description.Label, out _);

        if (extras.GetBoolean(ExtraSensitive, false))
            return true;
        return OperatingSystem.IsAndroidVersionAtLeast(33)
            && extras.GetBoolean(ClipDescription.ExtraIsSensitive, false);
    }

    static bool HasImageMime(ClipDescription description)
    {
        if (description.HasMimeType("image/*"))
            return true;

        for (var i = 0; i < description.MimeTypeCount; i++)
        {
            if (description.GetMimeType(i)?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        return false;
    }

    static bool HasFileMime(ClipDescription description)
    {
        for (var i = 0; i < description.MimeTypeCount; i++)
        {
            var mime = description.GetMimeType(i);
            if (string.IsNullOrWhiteSpace(mime))
                continue;
            if (mime.Equals(ClipDescription.MimetypeTextPlain, StringComparison.OrdinalIgnoreCase)
                || mime.Equals(ClipDescription.MimetypeTextHtml, StringComparison.OrdinalIgnoreCase)
                || mime.Equals(ClipDescription.MimetypeTextUrilist, StringComparison.OrdinalIgnoreCase)
                || mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                continue;
            return true;
        }

        return false;
    }

    static ClipData.Item? FirstItem()
    {
        var clip = Manager()?.PrimaryClip;
        return clip is { ItemCount: > 0 } ? clip.GetItemAt(0) : null;
    }

    static void SetClip(ClipData clip)
    {
        var clipboard = Manager() ?? throw new InvalidOperationException("Clipboard is not available.");
        clipboard.PrimaryClip = clip;
    }

    static ClipboardManager? Manager() =>
        AppContext().GetSystemService(Context.ClipboardService) as ClipboardManager;

    static Context AppContext() =>
        Android.App.Application.Context
        ?? throw new InvalidOperationException("Android application context is not available.");

    static JavaFile WriteCacheFile(string id, string extension, byte[] data)
    {
        var directory = new JavaFile(AppContext().CacheDir, "clipboardplus");
        directory.Mkdirs();
        var file = new JavaFile(directory, id + extension);
        File.WriteAllBytes(file.AbsolutePath, data);
        return file;
    }

    static AndroidUri ToShareableUri(JavaFile file)
    {
        var context = AppContext();
        try
        {
            return AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileProvider", file)
                ?? throw new InvalidOperationException("FileProvider did not return a URI.");
        }
        catch (Java.Lang.Exception)
        {
            return AndroidUri.FromFile(file)
                ?? throw new InvalidOperationException("Could not create a shareable file URI.");
        }
    }

    static string ExtensionFor(string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".png"
    };

    static bool TryCreateUri(string? value, [NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri);

    static bool IsWebOrAbsolute(Uri uri) =>
        uri.IsAbsoluteUri && (IsWeb(uri.ToString()) || uri.Scheme is "http" or "https" or "mailto" or "tel");

    static bool IsWeb(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

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

    sealed class ClipListener : Java.Lang.Object, ClipboardManager.IOnPrimaryClipChangedListener
    {
        public Action? Changed { get; set; }

        public void OnPrimaryClipChanged() => Changed?.Invoke();
    }
}
#endif
