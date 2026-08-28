using Microsoft.Maui.Hosting;

namespace Plugin.Maui.ClipboardPlus;

sealed class ClipboardPlusInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var clipboard = services.GetService<IClipboardPlus>() ?? ClipboardPlus.Current;
        ClipboardPlus.SetDefault(clipboard);
        clipboard.Start();
    }
}
