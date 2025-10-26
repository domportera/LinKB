#undef USE_EVENT_HOOKS
using LinKb.Core;

namespace LinKb.Application;

#pragma warning disable CA1859
internal static class Daemon
{
    internal static readonly AutoResetEvent InputTrigger = new(false);

    public static async Task Run(MidiKeyboardGrid grid, KeyHandler keyHandler, IApplication app)
    {
        var cts = new CancellationTokenSource();
        var inputTask = Task.Run(() => ExecuteInputs(grid, keyHandler, cts.Token));
        
        var mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
        try
        {
            app.Run(mainContext);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        Log.Debug("Application logic stopping");

        if (!inputTask.IsCompleted)
        {
            await Task.WhenAll(cts.CancelAsync(), inputTask);
        }
        
        cts.Dispose();
        Log.Debug("Application stopped");
    }

    private static void ExecuteInputs(MidiKeyboardGrid grid, KeyHandler keyHandler, CancellationToken token)
    {
        var waitTime = Math.Max(keyHandler.RepeatRateMs / 4, 1);
        while (!token.IsCancellationRequested)
        {
            // simulate key updates
            InputTrigger.WaitOne(waitTime);
            grid.ProcessEventQueue();
            keyHandler.UpdateRepeats();
        }
    }
}

#pragma warning restore CA1859