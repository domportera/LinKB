#undef USE_EVENT_HOOKS
using LinKb.Core;

namespace LinKb.Application;

#pragma warning disable CA1859
internal class Daemon
{
    internal static readonly AutoResetEvent InputTrigger = new(false);

    public static Task Run(MidiKeyboardGrid grid, KeyHandler keyHandler, IApplication app)
    {
        var appTask = app.Run();

        var waitTime = Math.Max(keyHandler.RepeatRateMs / 4, 1);
        while (appTask is { IsCompleted: false, IsFaulted: false, IsCanceled: false, IsCompletedSuccessfully: false })
        {
            // simulate key updates
            InputTrigger.WaitOne(waitTime);
            grid.ProcessEventQueue();
            keyHandler.UpdateRepeats();
        }
        
        Log.Debug("Application stopped");
        return Task.CompletedTask;
    }
}

#pragma warning restore CA1859