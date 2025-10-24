using System.Diagnostics;
using InputHooks;
using Microsoft.Extensions.Logging;

namespace LinKb.Core;

public readonly record struct KeyPress(KeyCode Key, bool Pressed);

internal class KeyHandler: IDisposable
{
    private readonly IEventSimulator _eventSimulator;
    private readonly IEventProvider _nativeInput;
    private readonly Stopwatch _stopwatch;
    public event EventHandler<KeyPress>? KeyEventTriggered;

    public bool IsPressed(KeyCode keycode) => _pressCounts[(int)keycode] > 0;

    public KeyHandler(IEventProvider nativeInput, IEventSimulator eventSimulator)
    {
        _nativeInput = nativeInput;
        _eventSimulator = eventSimulator;
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        
        _nativeInput.InputEventReceived += OnInputEventReceived;
    }

    public void Dispose()
    {
        _nativeInput.InputEventReceived -= OnInputEventReceived;
    }

    private void OnInputEventReceived(KeyboardEventArgs obj)
    {
        if (obj.KeyCodes.Length == 0)
            return;

        if (obj.KeyCodes.Length > 1)
        {
            Log.Debug($"Received keyboard event for {obj.KeyCodes.Length} keys: {(obj.IsDown ? "press" : "release")}");
        }
        else if(obj.KeyCodes.Length == 1)
        {
            Log.Debug($"Received keyboard event for {obj.KeyCodes[0]}: {(obj.IsDown ? "press" : "release")}");
        }
        
        // todo - differentiate between system keys and our emulated keys so we don't double up on
        // native key repeat events
    }

    public void UpdateRepeats()
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


    public void HandleKeyPress(KeyCode keycode, bool pressed)
    {
        if (pressed)
        {
            PressKey(keycode);
        }
        else
        {
            ReleaseKey(keycode);
        }
    }

    private bool PressKey(KeyCode keycode)
    {
        int keycodeInt = (int)keycode;
        ref var pressCount = ref _pressCounts[keycodeInt];
        if (++pressCount == 1)
        {
            _keyPressedTimesTicks[keycodeInt] = _stopwatch.ElapsedTicks;
            if (keycode != KeyCode.Undefined && keycode < KeyCode.NonSystemKeyStart)
            {
                _eventSimulator.SimulateKeyDown(keycode);
                try
                {
                    KeyEventTriggered?.Invoke(this, new KeyPress(keycode, true));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                return true;
            }
        }

        #if DEBUG
        if (pressCount > 1)
        {
            Log.Debug($"Key {keycode} press count: {pressCount}");
        }
        #endif

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keycode"></param>
    /// <returns>True if the key was fully released, false if the key is still held</returns>
    private bool ReleaseKey(KeyCode keycode)
    {
        var keycodeInt = (int)keycode;
        ref var pressCount = ref _pressCounts[keycodeInt];
        --_totalKeyPressCount;
        if (--pressCount == 0)
        {
            _keyPressedTimesTicks[keycodeInt] = NotPressedTime;
            _repeatsSent[keycodeInt] = 0;
            if (keycode != KeyCode.Undefined && keycode < KeyCode.NonSystemKeyStart)
            {
                _eventSimulator.SimulateKeyUp(keycode);

                try
                {
                    KeyEventTriggered?.Invoke(this, new KeyPress(keycode, false));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            return true;
        }

        if (pressCount < 0)
        {
            Log.Error($"Key {keycode} was released more times than it was pressed");
        }

        return false;
    }

    private long _totalKeyPressCount;
    internal int RepeatRateMs { get; private set; } = DefaultAutoRepeatRateMs;

    private const int DefaultAutoRepeatDelayMs = 500;
    private const int DefaultAutoRepeatRateMs = 33;
    private ulong _autoRepeatDelayTicks = (ulong)(DefaultAutoRepeatDelayMs * TicksPerMillisecond);
    private ulong _autoRepeatRateTicks = (ulong)(DefaultAutoRepeatRateMs * TicksPerMillisecond);

    private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000d;
    private readonly ulong[] _repeatsSent = new ulong[ushort.MaxValue + 1];
    private readonly long[] _keyPressedTimesTicks = new long[ushort.MaxValue + 1];
    private readonly int[] _pressCounts = new int[ushort.MaxValue + 1];
    private const long NotPressedTime = 0;
}