namespace Plugin.Maui.ClipboardPlus;

sealed record SensitiveClipMarker(
    string Id,
    ClipboardContentKind Kind,
    DateTimeOffset? ExpiresAt,
    bool IsSensitive)
{
    public const string Prefix = "me.clipboardplus:";

    public static SensitiveClipMarker Create(ClipboardContentKind kind, bool isSensitive, DateTimeOffset? expiresAt) =>
        new(Guid.NewGuid().ToString("N"), kind, expiresAt, isSensitive);

    public string ToAndroidLabel(string? userLabel)
    {
        var token = Prefix + Id;
        return string.IsNullOrWhiteSpace(userLabel) ? token : userLabel.Trim() + "|" + token;
    }

    public static bool TryGetIdFromAndroidLabel(string? label, [NotNullWhen(true)] out string? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(label))
            return false;

        var index = label.LastIndexOf(Prefix, StringComparison.Ordinal);
        if (index < 0)
            return false;

        id = label[(index + Prefix.Length)..];
        return id.Length > 0;
    }
}
