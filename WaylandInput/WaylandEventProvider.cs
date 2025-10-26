using InputHooks;

namespace WaylandInput;

internal class WaylandEventProvider : IEventProvider
{
    private readonly CancellationTokenSource _cts = new();

    public WaylandEventProvider(WaylandInput input)
    {
        // needs to listen async
        var args = new ThreadArgs
        {
            CancellationToken = _cts.Token,
            Input = input
        };
        var thread = new Thread(Main!) { IsBackground = true };
        thread.Start(args);
    }

    public event Action<KeyboardEventArgs>? InputEventReceived;

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private class ThreadArgs
    {
        public required CancellationToken CancellationToken { get; init; }
        public readonly AutoResetEvent AutoResetEvent = new(false);
        public required WaylandInput Input { get; init; }
    }

    private void Main(object argsObj)
    {
        var args = (ThreadArgs)argsObj;
        var events = new List<(WaylandInput.InputEvent Event, int DeviceId, bool IsVirtual)>();

        var input = args.Input;
        var waitTimespan = TimeSpan.FromMicroseconds(200);
        Span<KeyCode> keys = stackalloc KeyCode[16];
        while (!args.CancellationToken.IsCancellationRequested)
        {
            input.ReadKeyboardEvents(events);

            if (events.Count <= 0)
            {
                if (!Thread.Yield())
                {
                    Thread.Sleep(waitTimespan);
                }

                continue;
            }

            // todo - detect duplicate events?
            foreach (var (evt, deviceId, isVirtual) in events)
            {
                if (!KeycodeMap.ToKeyCode.TryGetValue((LinuxKC)evt.code, out var keyCodes))
                {
                    Console.WriteLine($"Failed to find key code for {(LinuxKC)evt.code}");
                    continue;
                }
                
                keyCodes.CopyTo(keys);
                var span = keys[..keyCodes.Length];

                try
                {
                    InputEventReceived?.Invoke(new KeyboardEventArgs
                    {
                        DeviceId = deviceId,
                        Timestamp = evt.time.Microseconds,
                        KeyCodes = span,
                        IsDown = evt.value is 1 or 2, // should we skip repeats? (2)
                        Pressure01 = evt.value == 1 ? 1 : 0,
                        IsSimulated = isVirtual
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

            events.Clear();
        }

        //input.Dispose();
    }
}