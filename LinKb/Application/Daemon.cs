#undef USE_EVENT_HOOKS
using LinKb.Core;

namespace LinKb.Application;

#pragma warning disable CA1859
internal class Daemon
{
    private static readonly AutoResetEvent RepeatEvent = new(false);

    public static Task Run(MidiKeyboardGrid grid, IApplication app)
    {
        var appTask = app.Run();

        var waitTime = Math.Max(grid.RepeatRateMs / 4, 1);
        while (appTask is { IsCompleted: false, IsFaulted: false, IsCanceled: false, IsCompletedSuccessfully: false })
        {
            // simulate key updates
            RepeatEvent.WaitOne(waitTime);
            grid.UpdateRepeats();
        }
        
        Log.Debug("Application stopped");
        return Task.CompletedTask;
    }
}

#pragma warning restore CA1859