using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using InputHooks;
using LinKb.Configuration;
using Midi.Net;

namespace LinKb.Core;

public partial class MidiKeyboardGrid
{
    #region Public

    public int Width => _config.Width;
    public int Height => _config.Height;
    public Layer Layer => _layer;

    public bool EnableKeyEvents = true;
    public bool IsPadPressed(int x, int y) => _pads[x, y].IsPressed;
    public bool IsKeyPressed(KeyCode key) => _keyPressedTimesTicks[(int)key] != NotPressedTime;

    public KeyCode GetKey(int x, int y, out Layer foundLayer, Layer? layer = null) =>
        _config.GetKey(x, y, layer ?? Layer, out foundLayer);

    public Vector3 GetAxes(int col, int row) => _pads[col, row].Axes;

    public ReadOnlySpan3D<KeyCode> Keymap => _config.Keymap;

    public bool TrySetKey(int col, int row, Layer layer, KeyCode key, [NotNullWhen(false)] out string? reason)
    {
        if (_config.SetKey(col, row, layer, key, out reason))
        {
            UpdateLED(col, row);
            return true;
        }

        return false;
    }

    public void ApplyKeymap(ReadOnlySpan3D<KeyCode> loaded)
    {
        _config.SetKeymap(0, loaded);
        _ledHandler?.UpdateAndPushAll(_config.Width, _config.Height);
    }

    private void UpdateLED(int col, int row)
    {
        if (_ledHandler == null)
            return;

        _ledHandler.UpdateButtonState(col, row);
        _ledHandler.PushLEDs();
    }


    #endregion Public

    internal Span3D<KeyCode> KeymapRW => _config.KeymapRW;
    internal int RepeatRateMs { get; private set; } = DefaultAutoRepeatRateMs;

    internal MidiKeyboardGrid(MidiDevice device, KeyboardGridConfig config, IEventSimulator eventSimulator)
    {
        // todo - get device connection/reconnection delegates that return midi device instead 
        // of providing it via the constructor, that way we can maintain the same grid object throughout disconnects?
        // or, create a way to create new grids throughout the lifetime of the application
        if (device is not IGridController)
        {
            throw new ArgumentException("Midi device must implement IGridController");
        }

        _stopwatch = new Stopwatch();
        _stopwatch.Start();

        _pads = new PadStatus[config.Width, config.Height];
        _pads.InitializeAsDefault();

        _config = config;
        _eventSimulator = eventSimulator;
        _device = device;
        if (device is ILEDGrid ledGrid)
        {
            _ledHandler = new LEDHandler(ledGrid, GetColor);
            _ledHandler.UpdateAndPushAll(_config.Width, _config.Height);
        }

        BeginReceiveThread();
    }

    internal void ApplyAutoRepeatSettings(int? repeatDelay, int? repeatRate)
    {
        if (repeatDelay is > 0)
        {
            _autoRepeatDelayTicks = (ulong)(repeatDelay.Value * TicksPerMillisecond);
            Log.Info("Auto repeat delay set to " + repeatDelay);
        }

        if (repeatRate is > 0)
        {
            RepeatRateMs = repeatRate.Value;
            _autoRepeatRateTicks = (ulong)(repeatRate * TicksPerMillisecond);
            Log.Info("Auto repeat rate set to " + repeatRate);
        }
    }

    private void UpdateRepeats()
    {
        // check key times and simulate repeats as needed
        var nowTicks = _stopwatch.ElapsedTicks;

        // todo: simd?
        var autoRepeatDelayTicksSigned = (long)_autoRepeatDelayTicks;
        var maxIdx = (int)KeyCode.NonSystemKeyStart;
        for (var i = 0; i < maxIdx; i++)
        {
            var pressTime = _keyPressedTimesTicks[i];
            if (pressTime == NotPressedTime)
                continue;
            var elapsed = nowTicks - pressTime;
            if (elapsed < autoRepeatDelayTicksSigned)
                continue;

            var repeatsSent = _repeatsSent[i];
            bool shouldRepeat;

            unchecked
            {
                // if we have not repeated OR (firstRepeatTime) / repeatRate > repeatsSent
                if (repeatsSent == 0 || ((ulong)elapsed - _autoRepeatDelayTicks) / _autoRepeatRateTicks > repeatsSent)
                {
                    ++_repeatsSent[i];
                    shouldRepeat = true;
                }
                else
                {
                    shouldRepeat = false;
                }
            }

            if (shouldRepeat)
            {
                _eventSimulator.SimulateKeyRepeat((KeyCode)i);
            }
        }
    }

    internal void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void OnPadPress(bool pressed, int keyX, int keyY)
    {
        if (!EnableKeyEvents)
        {
            PushLED(keyX, keyY);
            return;
        }

        var keycode = _config.GetKey(keyX, keyY, Layer, out var foundLayer);
        Log.Debug(
            $"Key {(pressed ? "Pressed" : "Released")}: {KeyInfo.ToName[keycode]} at ({keyX},{keyY}) on {Layer} from {foundLayer}");
        var currentKeyPressTime = _keyPressedTimesTicks[(int)keycode];
        var wasPressed = currentKeyPressTime != NotPressedTime;
        if (pressed != wasPressed)
        {
            if (pressed)
            {
                _keyPressedTimesTicks[(int)keycode] = _stopwatch.ElapsedTicks;
            }
            else
            {
                _keyPressedTimesTicks[(int)keycode] = NotPressedTime;
                _repeatsSent[(int)keycode] = 0;
            }

            if (keycode is not KeyCode.Undefined)
            {
                RaiseKeyEvent(keycode, pressed, _ledHandler, ref _layer, _keyPressedTimesTicks, _repeatsSent,
                    _eventSimulator, _config);
            }
        }

        PushLED(keyX, keyY);

        return;

        void PushLED(int i, int keyY1)
        {
            if (_ledHandler is null) return;

            _ledHandler.UpdateButtonState(i, keyY1);
            _ledHandler.PushLEDs();
        }
    }

    private static void RaiseKeyEvent(KeyCode kc, bool isPress, LEDHandler? leds, ref Layer currentLayer,
        long[] keyPressedTimesTicks, ulong[] repeatCounts, IEventSimulator eventSimulator, KeyboardGridConfig config)
    {
        // non-modifier keys
        if (kc < KeyCode.NonSystemKeyStart)
        {
            if (isPress)
            {
                eventSimulator.SimulateKeyDown(kc);
            }
            else
            {
                eventSimulator.SimulateKeyUp(kc);
            }

            return;
        }

        if (kc < KeyCode.ModifierKeyMin)
            return;

        var previousLayer = currentLayer;

        // get new modifier state
        // todo - more normal way to get the mod keys - whats the "standardized" way to do this?
        var mod1 = keyPressedTimesTicks[(int)KeyCode.Mod1] != NotPressedTime;
        var mod2 = keyPressedTimesTicks[(int)KeyCode.Mod2] != NotPressedTime;
        var mod3 = keyPressedTimesTicks[(int)KeyCode.Mod3] != NotPressedTime;
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
                if (newLayer > previousLayer &&
                    newLayerKey == KeyCode.Undefined) // no layers have a key for this pad
                    continue;

                var prevLayerKey = config.GetKey(x, y, previousLayer, out _);
                if (prevLayerKey == newLayerKey) // ignore the keys that match that of the new layer
                    continue;


                var prevLayerKeyInt = (int)prevLayerKey;
                var isPreviousKeyPressed = keyPressedTimesTicks[prevLayerKeyInt];
                if (isPreviousKeyPressed == NotPressedTime)
                {
                    // update the LED state for this key
                    leds?.UpdateButtonState(x, y);
                    continue;
                }

                // release the key
                keyPressedTimesTicks[prevLayerKeyInt] = NotPressedTime;
                repeatCounts[prevLayerKeyInt] = 0;
                eventSimulator.SimulateKeyUp(prevLayerKey);

                // update the LED state for this key
                leds?.UpdateButtonState(x, y);
                Log.Info($"Key {prevLayerKey} released due to layer change");
            }
        }
    }


    private void Dispose(bool _)
    {
        _device.MidiReceived -= OnMidiReceived;
        _cancellationTokenSource.CancelAsync().Wait();
        _eventWaitHandle.Dispose();
    }

    ~MidiKeyboardGrid()
    {
        Dispose(false);
    }

    // todo - this should be what returns from the midi event abstraction
    // when the logic is moved to the Linnstrument project
    private readonly PadStatus[,] _pads;
    private const int DefaultAutoRepeatDelayMs = 500;
    private const int DefaultAutoRepeatRateMs = 33;
    private ulong _autoRepeatDelayTicks = (ulong)(DefaultAutoRepeatDelayMs * TicksPerMillisecond);
    private ulong _autoRepeatRateTicks = (ulong)(DefaultAutoRepeatRateMs * TicksPerMillisecond);

    private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000d;
    private readonly ulong[] _repeatsSent = new ulong[ushort.MaxValue + 1];
    private readonly long[] _keyPressedTimesTicks = new long[ushort.MaxValue + 1];
    private const long NotPressedTime = 0;

    private Layer _layer;
    private readonly LEDHandler? _ledHandler;
    private readonly KeyboardGridConfig _config;
    private readonly IEventSimulator _eventSimulator;
    private readonly MidiDevice _device;
    private readonly Stopwatch _stopwatch;
}