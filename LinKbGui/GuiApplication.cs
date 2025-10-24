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

    public async Task Run()
    {
        if(_hooks is null || _grid is null)
            throw new InvalidOperationException("Application not initialized");
        
        SilkWindowProvider.Initialize(!OperatingSystem.IsWindows());
        IImguiWindowProvider provider = new SilkWindowProvider();
        var drawer = new KeyboardConfigWindow(_grid, _hooks);

        await provider.ShowAsync("LinKB GUI", drawer, new SimpleWindowOptions
        {
            Size = new Vector2(1600, 600),
            AlwaysOnTop = false,
            Vsync = true,
            SizeFlags = WindowSizeFlags.ResizeGui | WindowSizeFlags.ResizeWindow
        });
        
        Log.Info("GUI application has exited");
        await Task.Delay(1000);
    }
}