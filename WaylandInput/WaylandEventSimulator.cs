using InputHooks;

namespace WaylandInput;

internal class WaylandEventSimulator : IEventSimulator
{
    private readonly WaylandInput _input;
    public WaylandEventSimulator(WaylandInput input) => _input = input;


    public void SimulateKeyDown(KeyCode kc) =>
        _input.InjectKeyEvent(KeycodeMap.ToLinux[kc][0], WaylandInput.KeyEvent.Press);

    public void SimulateKeyUp(KeyCode kc) =>
        _input.InjectKeyEvent(KeycodeMap.ToLinux[kc][0], WaylandInput.KeyEvent.Release);

    public void SimulateKeyRepeat(KeyCode kc) =>
        _input.InjectKeyEvent(KeycodeMap.ToLinux[kc][0], WaylandInput.KeyEvent.Repeat);
}