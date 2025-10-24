using System.Reflection.Metadata.Ecma335;
using InputHooks;

namespace WaylandInput;

public class WaylandIOHandler : ISystemInputBase
{
    public int? AutoRepeatDelay => null;
    public int? AutoRepeatRate => null;

    public IEventSimulator EventSimulator => _eventSimulator ??= _input is null
        ? throw new InvalidOperationException("No input was initialized")
        : new WaylandEventSimulator(_input);

    public bool SupportsKey(KeyCode kc) => KeycodeMap.ToLinux.ContainsKey(kc);

    public Task Start(out IEventProvider provider)
    {
        _input = new WaylandInput();
        _input.Init();
        provider = _listener = new WaylandEventProvider(_input);
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        _listener?.Dispose();
        _input?.Dispose();
        _input = null;
        return Task.CompletedTask;
    }

    private WaylandEventProvider? _listener;
    private IEventSimulator? _eventSimulator;
    private WaylandInput? _input;
}