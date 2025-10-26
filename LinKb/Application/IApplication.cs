using InputHooks;
using LinKb.Core;

namespace LinKb.Application;

public interface IApplication
{
    void Initialize(IEventProvider hooks, MidiKeyboardGrid grid);
    void Run(SynchronizationContext mainContext);
}