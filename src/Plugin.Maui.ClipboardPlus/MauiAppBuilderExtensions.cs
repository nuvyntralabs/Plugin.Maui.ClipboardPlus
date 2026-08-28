using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace Plugin.Maui.ClipboardPlus;

/// <summary>
/// MAUI host registration for ClipboardPlus.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="IClipboardPlus"/> as a singleton and starts clipboard monitoring.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMauiClipboardPlus(options =>
    /// {
    ///     options.DefaultSensitiveExpiration = TimeSpan.FromMinutes(2);
    ///     options.ClearSensitiveOnAppBackground = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMauiClipboardPlus(this MauiAppBuilder builder, Action<ClipboardPlusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new ClipboardPlusOptions();
        configure?.Invoke(options);

        builder.Services.AddMauiClipboardPlus(options);
        builder.Services.AddTransient<IMauiInitializeService, ClipboardPlusInitializer>();

        if (options.ClearSensitiveOnAppBackground)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android.OnStop(activity =>
                {
                    ClearSensitiveInBackground();
                }));
#elif IOS
                events.AddiOS(ios => ios.OnResignActivation(app =>
                {
                    ClearSensitiveInBackground();
                }));
#endif
            });
        }

        return builder;
    }

    static void ClearSensitiveInBackground() =>
        _ = ClipboardPlus.Current.ClearSensitiveAsync();
}
