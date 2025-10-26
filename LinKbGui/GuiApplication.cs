using System.Numerics;
using ImGuiWindows;
using InputHooks;
using LinKb.Application;
using LinKb.Core;
using SilkWindows;

namespace LinKbGui;

internal class GuiApplication : IApplication
{
    private IEventProvider? _hooks;
    private MidiKeyboardGrid? _grid;
    public void Initialize(IEventProvider hooks, MidiKeyboardGrid grid)
    {
        _hooks = hooks;
        _grid = grid;
    }

    public void Run(SynchronizationContext mainContext)
    {
        if(_hooks is null || _grid is null)
            throw new InvalidOperationException("Application not initialized");
        
        var windowRunner = new WindowRunner(new SilkWindowProvider(), mainContext);
        var drawer = new KeyboardConfigWindow(_grid, _hooks);
        var window = windowRunner.Show("LinKB GUI", drawer, new SimpleWindowOptions
        {
            Size = new Vector2(1600, 600),
            AlwaysOnTop = false,
            Vsync = true,
            SizeFlags = WindowSizeFlags.ResizeGui | WindowSizeFlags.ResizeWindow
        });

        while (!window.IsCompleted)
        {
            windowRunner.MainThreadUpdate();
            windowRunner.Render();
        }

        Log.Info("GUI application has exited");
        
    }
}