namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// Registers ClipboardPlus services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IClipboardPlus"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddMauiClipboardPlus(this IServiceCollection services, ClipboardPlusOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IClipboardPlus>(sp =>
        {
            var resolved = sp.GetService<ClipboardPlusOptions>() ?? options;
            var clipboard = ClipboardPlus.Create(resolved);
            ClipboardPlus.SetDefault(clipboard);
            return clipboard;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="IClipboardPlus"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddMauiClipboardPlus(this IServiceCollection services, Action<ClipboardPlusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ClipboardPlusOptions();
        configure?.Invoke(options);
        return services.AddMauiClipboardPlus(options);
    }
}
