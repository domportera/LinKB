using ImGuiWindows;
using InputHooks;
using LinKb.Configuration;
using LinKb.Core;

namespace LinKb.Application;

public interface IApplication
{
    void Initialize(IEventProvider hooks, MidiKeyboardGrid grid, KeyboardGridConfig config);
    void Run(SynchronizationContext mainContext);
    
}

public readonly record struct WindowAction<T>(IImguiDrawer<T> Drawer, Action<T?>? OnClose = null);
public readonly record struct WindowAction(IImguiDrawer Drawer, Action? OnClose = null);