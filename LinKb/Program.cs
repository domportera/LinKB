using LinKb.Application;
using LinKb.Configuration;
using LinKb.Core;
using Linn;
using Midi.Net;

namespace LinKb;

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
public static class Main
{
    private enum ExitCodes
    {
        Success = 0,
        FailedToOpenDevice = 1,
        FailedToApplyUserFirmwareMode = 2
    }

    public static async Task<int> Run(string[] args, IApplication application)
    {
        var keys = await LayoutSerializer.LoadOrCreateKeymap(UserInfo.DefaultConfigFile, 25, 8, 8);
        var config = new KeyboardGridConfig(keys);
        var items = await KeySupport.Begin(config);
        
        var (gridDevice, midiDeviceStatus) = await TryOpenLinnstrument();

        if (midiDeviceStatus != ExitCodes.Success)
        {
            return (int)midiDeviceStatus;
        }

        var grid = new MidiKeyboardGrid(gridDevice!, config, items.Simulator);
        grid.ApplyAutoRepeatSettings(items.AutoRepeatDelay, items.AutoRepeatRate);

        application.Initialize(items.InputEventProvider, grid);
        await Daemon.Run(grid, application);
        Log.Info("Gui application exited");
        grid.Dispose();
        Log.Debug("Keyboard grid disposed");

        await KeySupport.End();
        
        _ = await TryCloseLinnstrument(gridDevice!);
        Log.Info("Linnstrument closed");

        return (int)ExitCodes.Success;
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
        var linnstrumentResult = await DeviceHandler.TryOpen<Linnstrument>(deviceSearchTerm);
        if (linnstrumentResult.Info != DeviceHandler.DeviceOpenResult.Success)
        {
            Log.Info("Failed to open MIDI device: " + linnstrumentResult.Info);
            return (null, ExitCodes.FailedToOpenDevice);
        }
        
        var linnstrument = linnstrumentResult.Device;
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
        await Task.Delay(1000);

        if (!linnstrument.TryApplyUserFirmwareMode(true))
        {
            Log.Error("Failed to apply user firmware mode");
            linnstrument.Dispose();
            return (null, ExitCodes.FailedToApplyUserFirmwareMode);
        }

        return (linnstrument, ExitCodes.Success);
    }
}
