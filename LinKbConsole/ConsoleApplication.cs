using InputHooks;
using LinKb.Application;
using LinKb.Core;

namespace KeyboardGUI.GUI;

internal class ConsoleApplication : IApplication
{
    private IEventProvider? _hooks;
    private MidiKeyboardGrid? _grid;
    public void Initialize(IEventProvider hooks, MidiKeyboardGrid grid)
    {
        _hooks = hooks;
        _grid = grid;
    }

    public async Task Run()
    {
        if(_hooks is null || _grid is null)
            throw new InvalidOperationException("Application not initialized");

        await Task.Run(() =>
        {
            Log.Info("LinKB application started - press Ctrl+C to exit.");
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();
        });
    }
}