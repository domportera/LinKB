namespace InputHooks;

public interface IEventSimulator
{
    void SimulateKeyDown(KeyCode kc);
    void SimulateKeyUp(KeyCode kc);
    void SimulateKeyRepeat(KeyCode kc);
}