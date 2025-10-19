namespace InputHooks;

public interface ISystemInputBase
{
    bool SupportsKey(KeyCode kc);
    Task Start(out IEventProvider provider);
    Task Stop();
    
    // key repeat info
    int? AutoRepeatDelay { get; }
    int? AutoRepeatRate { get; }
    
    IEventSimulator EventSimulator { get; }
}