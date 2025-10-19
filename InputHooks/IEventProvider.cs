namespace InputHooks;

public interface IEventProvider
{
    event Action<KeyboardEventArgs> InputEventReceived;
}