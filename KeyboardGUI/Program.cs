#undef USE_EVENT_HOOKS
using KeyboardGUI.Configuration;
using KeyboardGUI.Core;
using KeyboardGUI.GUI;
using Linn;
using Midi.Net;
using SharpHook;
using SharpHook.Providers;

namespace KeyboardGUI;

/// <summary>
/// Converts Linnstrument messages into keyboard events, using
/// <a href="https://github.com/rogerlinndesign/linnstrument-firmware/blob/master/user_firmware_mode.txt">this
/// documentation</a> as a guide.
/// Uses <a href="https://github.com/atsushieno/managed-midi">Managed-Midi (namespace CoreMidi)</a> for midi messages,
/// and <a href="https://sharphook.tolik.io/">SharpHook</a> for keyboard input spoofing.
/// 
/// The linnstrument's keymap is fully modular by changing what SharpHook KeyCode is assigned to any given pad
/// of the linnstrument.
///
/// pads of the linnstrument are populated via a 2D array,
/// with each entry corresponding to its relevant pad on the linnstrument pad grid.
///
/// Configurations are serialized in a plaintext file that serves as a "gui" of the linnstrument surface
/// </summary>
internal static class Program
{
    private enum ExitCodes
    {
        Success = 0,
        FailedToOpenDevice = 1,
        FailedToApplyUserFirmwareMode = 2
    }

    public static async Task<int> Main(string[] args)
    {
        var (gridDevice, midiDeviceStatus) = await TryOpenLinnstrument();

        if (midiDeviceStatus != ExitCodes.Success)
        {
            return (int)midiDeviceStatus;
        }

        var config = await LayoutSerializer.LoadOrCreateConfig(UserInfo.DefaultConfigFile);
        var grid = new MidiKeyboardGrid(gridDevice!, config);

        var hooks = CreateHooks(out var hookTask, out var delay, out var rate);
        grid.ApplyAutoRepeatSettings(delay, rate);
        await GuiApplication.Run(grid, hooks);
        Log.Info("Gui application exited, stopping hooks");
        await StopHooks(hooks, hookTask);
        Log.Info("Input hooks stopped");
        grid.Dispose();
        Log.Debug("Keyboard grid disposed");

        _ = await TryCloseLinnstrument(gridDevice!);
        Log.Info("Linnstrument closed");

        return (int)ExitCodes.Success;
    }

    private static async Task StopHooks(GlobalHookBase hooks, Task hookTask)
    {
        hooks.Stop();
        await hookTask;
        if (!hooks.IsDisposed)
        {
            hooks.Dispose();
        }
    }

    private static GlobalHookBase CreateHooks(out Task hookTask, out int repeatDelay, out int repeatRate)
    {
#if USE_EVENT_HOOKS
        GlobalHookBase hooks = new EventLoopGlobalHook(runAsyncOnBackgroundThread: true);
#else
        GlobalHookBase hooks = new SimpleGlobalHook(runAsyncOnBackgroundThread: true);
#endif

        repeatDelay = UioHookProvider.Instance.GetAutoRepeatDelay();
        repeatRate = UioHookProvider.Instance.GetAutoRepeatRate();
        hookTask = hooks.RunAsync();
        return hooks;
    }

    private static async Task<bool> TryCloseLinnstrument(Linnstrument linnstrument)
    {
        var success = linnstrument.TryApplyUserFirmwareMode(false);
        if (success)
        {
            await Task.Delay(200);
        }
        else
        {
            Log.Error("Failed to unset user firmware mode");
        }

        linnstrument.Dispose();
        return success;
    }

    private static async Task<(Linnstrument? linnstrument, ExitCodes failedToOpenDevice)> TryOpenLinnstrument()
    {
        const string deviceSearchTerm = "linnstrument";
        var linnstrument = await DeviceHandler.TryOpen<Linnstrument>(deviceSearchTerm);
        if (linnstrument is null)
        {
            Log.Info("Failed to open MIDI device");
        }

        if (linnstrument is null || !linnstrument.TryApplyUserFirmwareMode(true))
        {
            linnstrument?.Dispose();
            return (null, ExitCodes.FailedToOpenDevice);
        }

        Log.Info("Opened MIDI device " + linnstrument.Name);

        // give the device some time to process its connection state
        await Task.Delay(200);

        if (!linnstrument.TryApplyUserFirmwareMode(true))
        {
            Log.Error("Failed to apply user firmware mode");
            linnstrument.Dispose();
            return (null, ExitCodes.FailedToApplyUserFirmwareMode);
        }

        return (linnstrument, ExitCodes.Success);
    }
}