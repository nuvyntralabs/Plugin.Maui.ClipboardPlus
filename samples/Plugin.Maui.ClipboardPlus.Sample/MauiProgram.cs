using Microsoft.Extensions.Logging;
using Plugin.Maui.ClipboardPlus;

namespace Plugin.Maui.ClipboardPlus.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMauiClipboardPlus(options =>
            {
                options.DefaultSensitiveExpiration = TimeSpan.FromMinutes(2);
                options.ClearSensitiveOnAppBackground = true;
                options.MonitorClipboard = true;
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
