#undef USE_EVENT_HOOKS
using System.Numerics;
using ImGuiWindows;
using KeyboardGUI.Core;
using SharpHook;
using SilkWindows;

namespace KeyboardGUI.GUI;

#pragma warning disable CA1859
internal static class GuiApplication
{
    private static readonly AutoResetEvent RepeatEvent = new(false);

    public static async Task Run(MidiKeyboardGrid grid, GlobalHookBase hooks)
    {
        IImguiWindowProvider provider = new SilkWindowProvider();
        var drawer = new KeyboardConfigWindow(grid, hooks);

        var guiTask = provider.ShowAsync("Linnstrument Keyboard", drawer, new SimpleWindowOptions
        {
            Size = new Vector2(1600, 600),
            AlwaysOnTop = false,
            Vsync = true,
            SizeFlags = WindowSizeFlags.ResizeGui | WindowSizeFlags.ResizeWindow
        });

        var waitTime = Math.Max(grid.RepeatRateMs / 4, 1);
        while (!guiTask.IsCompleted)
        {
            // simulate key updates
            RepeatEvent.WaitOne(waitTime);
            grid.UpdateRepeats();
        }

        await Task.Delay(100); // hack - gives Silk.NET window time to shut down properly
    }
}

#pragma warning restore CA1859