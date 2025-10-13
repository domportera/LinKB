using System.Diagnostics;
using KeyboardGUI.Configuration;
using KeyboardGUI.Keys;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;
using SharpHook;
using SharpHook.Data;

namespace KeyboardGUI.Core;

internal class MidiKeyboardGrid : IDisposable
{
    private readonly bool[,] _padStates;
    private readonly KeyboardGridConfig _config;
    private readonly EventSimulator _eventSimulator;
    private readonly MidiDevice _device;

    public KeyboardGridConfig Config => _config;

    public Layer Layer => _layer;
    private Layer _layer;

    public bool EnableKeyEvents = true;

    private readonly LEDHandler? _ledHandler;

    public bool IsKeyPressed(int x, int y) => _padStates[x, y];

    private readonly long[] _keyPressedTimes = new long[ushort.MaxValue + 1];

    private readonly Stopwatch _stopwatch;

    public MidiKeyboardGrid(MidiDevice device, KeyboardGridConfig config)
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _padStates = new bool[config.Width, config.Height];
        _pads = new PadStatus[config.Width, config.Height]; // 1-based indexing for pads
        _config = config;
        _eventSimulator = new EventSimulator();
        _device = device;
        if (device is ILEDGrid ledGrid)
        {
            _ledHandler = new LEDHandler(ledGrid, (x, y) =>
            {
                var keycode = _config.GetKey(x, y, Layer, out _);
                if (keycode == KeyCode.VcUndefined)
                {
                    return _config.Colors.UnlitColor;
                }

                var keyInt = (int)keycode;
                var isPressed = _keyPressedTimes[keyInt] != NotPressedTime;
                if (isPressed)
                {
                    return _config.Colors.PressedColor;
                }

                if (keycode >= KeyExtensions.ModifierKeyMin)
                    return _config.Colors.ModKeyColor;

                var isLockKey = keycode is KeyCode.VcCapsLock or KeyCode.VcNumLock or KeyCode.VcScrollLock;
                if (isLockKey)
                {
                    // todo - get actual lock status and color accordingly
                    return _config.Colors.LockedColor;
                }

                var isNormal = keycode.IsNormal();
                return isNormal ? _config.Colors.LitColor : _config.Colors.SpecialLitColor;
            });

            _ledHandler.UpdateAndPushAll(_config.Width, _config.Height);
        }

        _device.MidiReceived += OnMidiReceived;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void OnMidiReceived(object? sender, MidiEvent e)
    {
        // Handle MIDI event
        var channel = e.Status.Channel;
        if (e.IsNoteOn)
        {
            var x = e.Data.B1 - 1;
            var velocity = e.Data.B2;
            ApplyKeyValues(x, channel, true, velocity);
        }
        else if (e.IsNoteOff)
        {
            var x = e.Data.B1 - 1;
            var velocity = e.Data.B2;
            ApplyKeyValues(x, channel, false, velocity);
        }
        else if (e.Status.Type == StatusType.ControlChange)
        {
            // Handle Control Change messages if needed
            Log.Info($"Control Change: Controller {e.Data.B1}, Value {e.Data.B2}");
        }
        else
        {
            // Other MIDI events can be handled here
            Log.Info(e);
        }
    }

    private void ApplyKeyValues(int x, int y, bool pressed, int velocity)
    {
        ref var pad = ref GetPad(x, y);
        if (pad.IsPressed != pressed)
        {
            OnKeyPressed(pressed, x, y);
        }

        pad.IsPressed = pressed;
        pad.Velocity = velocity;
    }

    private void OnKeyPressed(bool pressed, int keyX, int keyY)
    {
        ref var padState = ref _padStates[keyX, keyY];
        if (padState == pressed)
            return;

        padState = pressed;

        if (!EnableKeyEvents)
        {
            if (_ledHandler is not null)
            {
                _ledHandler.UpdateButtonState(keyX, keyY);
                _ledHandler.PushLEDs();
            }

            return;
        }

        var keycode = _config.GetKey(keyX, keyY, Layer, out var foundLayer);
        Log.Debug(
            $"Key {(pressed ? "Pressed" : "Released")}: {KeyNames.KeyToName[keycode]} at ({keyX},{keyY}) on {Layer} from {foundLayer}");
        ref var currentKeyPressTime = ref _keyPressedTimes[(int)keycode];
        var wasPressed = currentKeyPressTime != NotPressedTime;
        if (pressed != wasPressed)
        {
            if (pressed)
            {
                currentKeyPressTime = _stopwatch.ElapsedTicks;
            }
            else
            {
                currentKeyPressTime = NotPressedTime;
                _repeatsSent[(int)keycode] = 0;
            }

            if (keycode is not KeyCode.VcUndefined)
            {
                RaiseKeyEvent(keycode, pressed, _ledHandler, ref _layer, _keyPressedTimes, _repeatsSent,
                    _eventSimulator, _config);
            }
        }

        if (_ledHandler is not null)
        {
            _ledHandler.UpdateButtonState(keyX, keyY);
            _ledHandler.PushLEDs();
        }

        return;

        static void RaiseKeyEvent(KeyCode kc, bool isPress, LEDHandler? leds, ref Layer currentLayer,
            long[] keyPressedState, ulong[] repeatCounts, EventSimulator eventSimulator, KeyboardGridConfig config)
        {
            // non-modifier keys
            if (kc < KeyExtensions.ModifierKeyMin)
            {
                if (isPress)
                {
                    eventSimulator.SimulateKeyPress(kc);
                }
                else
                {
                    eventSimulator.SimulateKeyRelease(kc);
                }

                return;
            }

            var previousLayer = currentLayer;

            // get new modifier state
            var mod1 = keyPressedState[(int)KeyExtensions.Mod1] != NotPressedTime;
            var mod2 = keyPressedState[(int)KeyExtensions.Mod2] != NotPressedTime;
            var mod3 = keyPressedState[(int)KeyExtensions.Mod3] != NotPressedTime;
            var newLayer = Layer.Layer1;
            if (mod1)
                newLayer |= Layer.Layer2;
            if (mod2)
                newLayer |= Layer.Layer3;
            if (mod3)
                newLayer |= Layer.Layer4;

            if (previousLayer == newLayer)
            {
                return; // no change in layer
            }

            currentLayer = newLayer;

            Log.Debug("Layer changed to " + currentLayer);

            // release keys that have been switched via layer change
            // make sure to ignore the modifier keys themselves
            // also update LED
            var width = config.Width;
            var height = config.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var newLayerKey = config.GetKey(x, y, newLayer, out _);
                    if (newLayerKey == KeyCode.VcUndefined) // no layers have a key for this pad
                        continue;

                    var prevLayerKey = config.GetKey(x, y, previousLayer, out _);
                    if (prevLayerKey == newLayerKey) // ignore the keys that match that of the new layer
                        continue;


                    ref var isPreviousKeyPressed = ref keyPressedState[(int)prevLayerKey];
                    if (isPreviousKeyPressed == NotPressedTime)
                    {
                        // update the LED state for this key
                        leds?.UpdateButtonState(x, y);
                        continue;
                    }

                    // release the key
                    isPreviousKeyPressed = NotPressedTime;
                    repeatCounts[(int)prevLayerKey] = 0;
                    eventSimulator.SimulateKeyRelease(prevLayerKey);

                    // update the LED state for this key
                    leds?.UpdateButtonState(x, y);
                    Log.Info($"Key {prevLayerKey} released due to layer change");
                }
            }
        }
    }


    private void Dispose(bool _)
    {
        _device.MidiReceived -= OnMidiReceived;
    }

    ~MidiKeyboardGrid()
    {
        Dispose(false);
    }

    private ref PadStatus GetPad(int x, int y) => ref _pads[x, y];
    private readonly PadStatus[,] _pads;

    public bool IsKeyPressed(KeyCode key) => _keyPressedTimes[(int)key] != NotPressedTime;

    public void UpdateLED(int col, int row)
    {
        if (_ledHandler == null)
            return;

        _ledHandler.UpdateButtonState(col, row);
        _ledHandler.PushLEDs();
    }

    public void ApplyAutoRepeatSettings(int repeatDelay, int repeatRate)
    {
        if (repeatDelay > 0)
        {
            _autoRepeatDelayTicks = (ulong)(repeatDelay * TicksPerMillisecond);
            Log.Info("Auto repeat delay set to " + repeatDelay);
        }

        if (repeatRate > 0)
        {
            RepeatRateMs = repeatRate;
            _autoRepeatRateTicks = (ulong)(repeatRate * TicksPerMillisecond);
            Log.Info("Auto repeat rate set to " + repeatRate);
        }
    }

    public int RepeatRateMs { get; private set; } = DefaultAutoRepeatRateMs;
    private const int DefaultAutoRepeatDelayMs = 500;
    private const int DefaultAutoRepeatRateMs = 33;
    private ulong _autoRepeatDelayTicks = (ulong)(DefaultAutoRepeatDelayMs * TicksPerMillisecond);
    private ulong _autoRepeatRateTicks = (ulong)(DefaultAutoRepeatRateMs * TicksPerMillisecond);

    private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000d;
    private ulong[] _repeatsSent = new ulong[ushort.MaxValue + 1];
    private const long NotPressedTime = 0;

    public void UpdateRepeats()
    {
        // check key times and simulate repeats as needed
        var nowTicks = _stopwatch.ElapsedTicks;

        // todo: simd?
        var autoRepeatDelayTicksSigned = (long)_autoRepeatDelayTicks;
        for (var i = 0; i < _keyPressedTimes.Length; i++)
        {
            var pressTime = _keyPressedTimes[i];
            if (pressTime == NotPressedTime)
                continue;
            var elapsed = nowTicks - pressTime;
            if (elapsed < autoRepeatDelayTicksSigned)
                continue;

            ref var repeatsSent = ref _repeatsSent[i];
            bool shouldRepeat;

            unchecked
            {
                // if we have not repeated OR (firstRepeatTime) / repeatRate > repeatsSent
                if (repeatsSent == 0 || ((ulong)elapsed - _autoRepeatDelayTicks) / _autoRepeatRateTicks > repeatsSent)
                {
                    ++repeatsSent;
                    shouldRepeat = true;
                }
                else
                {
                    shouldRepeat = false;
                }
            }

            if (shouldRepeat)
            {
                _eventSimulator.SimulateKeyPress((KeyCode)i);
            }
        }
    }
}