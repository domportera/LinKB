#undef USE_EVENT_HOOKS
using System.Runtime.CompilerServices;
using InputHooks;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using IEventSimulator = InputHooks.IEventSimulator;
using KeyCode = InputHooks.KeyCode;

namespace SharphookInput;

public class SharphookSystemInput : ISystemInputBase
{
    public int? AutoRepeatDelay => _autoRepeatDelay;
    public int? AutoRepeatRate => _autoRepeatRate;


    public bool SupportsKey(KeyCode kc) => KeycodeMap.KeyCodeToHooksCode.ContainsKey(kc);

    public Task Start(out IEventProvider provider)
    {
        var a = KeycodeMap.HooksCodeToKeyCode[SharpHook.Data.KeyCode.Vc0];
        _hooks = CreateHooks(out var hookTask, out _autoRepeatDelay, out _autoRepeatRate);
        _hookTask = hookTask;
        provider = new EventProvider(_hooks);
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        _hooks?.Stop();
        if (_hookTask != null)
        {
            await _hookTask;
        }

        if (_hooks is { IsDisposed: false })
        {
            _hooks.Dispose();
        }
    }

    private static GlobalHookBase CreateHooks(out Task hookTask, out int repeatDelay, out int repeatRate)
    {
#if USE_EVENT_HOOKS
        GlobalHookBase hooks = new EventLoopGlobalHook(runAsyncOnBackgroundThread: true);
#else
        GlobalHookBase hooks = new SimpleGlobalHook(runAsyncOnBackgroundThread: true);
#endif

        repeatDelay = UioHookProvider.Instance.GetAutoRepeatDelay();
        repeatRate = UioHookProvider.Instance.GetAutoRepeatRate();
        hookTask = hooks.RunAsync();
        return hooks;
    }

    private GlobalHookBase? _hooks;
    private Task? _hookTask;
    private int _autoRepeatDelay;
    private int _autoRepeatRate;

    public IEventSimulator EventSimulator => _eventSimulator ??= new EventSimulatorImpl();
    private IEventSimulator? _eventSimulator;

    private class EventSimulatorImpl : IEventSimulator
    {
        private readonly EventSimulator _eventSimulator = new();
        public void SimulateKeyDown(KeyCode kc)
        {
            if (KeycodeMap.KeyCodeToHooksCode.TryGetValue(kc, out var hooksCode))
            {
                _eventSimulator.SimulateKeyPress(hooksCode);
            }
            else if(KeycodeMap.KeyCodeToMouseButton.TryGetValue(kc, out var mouseButton))
            {
                _eventSimulator.SimulateMousePress(mouseButton);
            }
        }

        public void SimulateKeyUp(KeyCode kc)
        {
            if (KeycodeMap.KeyCodeToHooksCode.TryGetValue(kc, out var hooksCode))
            {
                _eventSimulator.SimulateKeyRelease(hooksCode);
            }
            else if(KeycodeMap.KeyCodeToMouseButton.TryGetValue(kc, out var mouseButton))
            {
                _eventSimulator.SimulateMouseRelease(mouseButton);
            }
        }

        public void SimulateKeyRepeat(KeyCode kc) =>
            _eventSimulator.SimulateKeyPress(KeycodeMap.KeyCodeToHooksCode[kc]);
    }

    private class EventProvider : IEventProvider
    {
        public event Action<KeyboardEventArgs>? InputEventReceived;
        private readonly GlobalHookBase _hooks;

        public EventProvider(GlobalHookBase hooks)
        {
            _hooks = hooks;
            _hooks.KeyPressed += OnKeyPressed;
            _hooks.KeyReleased += OnKeyReleased;
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            Span<KeyCode> span = [GetKeyFor(e.Data.KeyCode)];
            CreateEventArgs(e, true, span, out var args);
            try
            {
                InputEventReceived?.Invoke(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            Span<KeyCode> span = [GetKeyFor(e.Data.KeyCode)];
            CreateEventArgs(e, false, span, out var args);
            try
            {
                InputEventReceived?.Invoke(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        private static void CreateEventArgs(KeyboardHookEventArgs e, bool isDown, Span<KeyCode> keyCode, out KeyboardEventArgs args)
        {
            args = new KeyboardEventArgs
            {
                DeviceId = e.IsEventSimulated ? int.MaxValue : 0,
                IsDown = isDown,
                KeyCodes = keyCode,
                Timestamp = e.EventTime.Ticks,
                Pressure01 = isDown ? 1 : 0,
                IsSimulated = e.IsEventSimulated
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static KeyCode GetKeyFor(SharpHook.Data.KeyCode key) => KeycodeMap.HooksCodeToKeyCode[key];
    }
}