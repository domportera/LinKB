using System.Runtime.CompilerServices;
using InputHooks;
using LinKb.Configuration;
using LinKb.Core;

namespace LinKb;

internal static class KeySupport
{

    private static ISystemInputBase? _hooks;
    public readonly record struct BeginResult(IEventProvider InputEventProvider, IEventSimulator Simulator, int? AutoRepeatDelay, int? AutoRepeatRate);
    public static async Task<BeginResult> Begin(KeyboardGridConfig config)
    {
        if(_hooks != null)
            throw new InvalidOperationException("Already started");

        if (OperatingSystem.IsLinux())
        {
            _hooks = new WaylandInput.WaylandIOHandler();
        }
        else
        {
            _hooks = new SharphookInput.SharphookSystemInput();
        }
        
        // validate config
        var keys = config.Keymap;
        for (var x = 0; x < keys.XLength; x++)
        {
            for(var y = 0; y < keys.YLength; y++)
            {
                for(var z = 0; z < keys.ZLength; z++)
                {
                    ref readonly var key = ref keys[x, y, z];
                    if (key is not KeyCode.Undefined and not KeyCode.Mod1 and not KeyCode.Mod2 and not KeyCode.Mod3 && !_hooks.SupportsKey(key))
                    {
                        Log.Error($"Key {key} is not supported on this platform - replacing with {KeyCode.Undefined.Name()}.");
                        config.SetKey(x, y, (Layer)z, KeyCode.Undefined, out _);
                    }
                }
            }
        }

        await _hooks.Start(out var eventProvider);
        return new BeginResult(eventProvider, _hooks.EventSimulator, _hooks.AutoRepeatDelay, _hooks.AutoRepeatRate);
    }

    public static async Task End()
    {
        await _hooks!.Stop();
        _hooks = null;
        Log.Info("Input hooks stopped");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupported(this KeyCode key) => _hooks!.SupportsKey(key);
}