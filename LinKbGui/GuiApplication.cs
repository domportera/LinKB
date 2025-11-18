using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiWindows;
using InputHooks;
using LinKb.Application;
using LinKb.Configuration;
using LinKb.Core;
using SilkWindows;

namespace LinKbGui;

internal class GuiApplication : IApplication
{
    private IEventProvider? _hooks;
    private MidiKeyboardGrid? _grid;
    private KeyboardGridConfig? _config;
    
    [MemberNotNullWhen(true, nameof(_hooks), nameof(_grid), nameof(_config))]
    private bool Initialized { get; set; }
    
    
    public void Initialize(IEventProvider hooks, MidiKeyboardGrid grid, KeyboardGridConfig config)
    {
        if(Initialized)
            throw new InvalidOperationException("Application already initialized");
        
        _hooks = hooks;
        _grid = grid;
        _config = config;
        Initialized = true;
    }

    public void Run(SynchronizationContext mainContext)
    {
        if(!Initialized)
            throw new InvalidOperationException("Application not initialized");
        
        var windowRunner = new WindowRunner(new SilkWindowProvider(), mainContext);
        var profileGui = new ProfileGui(_config, windowRunner);
        var drawer = new KeyboardConfigWindow(_grid, profileGui);
        var window = windowRunner.Show("LinKB GUI", drawer, new SimpleWindowOptions
        {
            Size = new Vector2(1600, 600),
            AlwaysOnTop = false,
            Vsync = true,
            SizeFlags = WindowSizeFlags.ResizeGui | WindowSizeFlags.ResizeWindow
        });

        while (!window.IsCompleted)
        {
            try
            {
                windowRunner.MainThreadUpdate();
                windowRunner.Render();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        Log.Info("GUI application has exited");
        
    }
}