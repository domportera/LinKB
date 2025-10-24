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
    public static async Task<ExitCodes> Run(string[] args, IApplication application)
    {
        var keys = await LayoutSerializer.LoadOrCreateKeymap(UserInfo.DefaultConfigFile, 25, 8, 8);
        var config = new KeyboardGridConfig(keys);
        var items = await KeySupport.Begin(config);

        var (gridDevice, midiDeviceStatus) = await TryOpenLinnstrument();

        if (midiDeviceStatus != ExitCodes.Success)
        {
            return midiDeviceStatus;
        }

        var grid = new MidiKeyboardGrid(gridDevice!, config, items.Simulator);
        grid.ApplyAutoRepeatSettings(items.AutoRepeatDelay, items.AutoRepeatRate);

        application.Initialize(items.InputEventProvider, grid);
        await Daemon.Run(grid, application);
        Log.Info("Gui application exited");
        grid.Dispose();
        Log.Debug("Keyboard grid disposed");

        await KeySupport.End();
        await gridDevice!.CloseAsync();

        return ExitCodes.Success;
    }


    private static async Task<(MidiDevice? linnstrument, ExitCodes failedToOpenDevice)> TryOpenLinnstrument()
    {
        const string deviceSearchTerm = "linnstrument";
        var (result, linnstrument) = await DeviceHandler.TryOpen<Linnstrument>(deviceSearchTerm);
        
        if (result != DeviceHandler.DeviceOpenResult.Success)
        {
            Log.Info("Failed to open MIDI device: " + result);
            return (null, ExitCodes.FailedToOpenDevice);
        }

        if (linnstrument is null)
        {
            Log.Info("Failed to open MIDI device");
        }

        if (linnstrument is null)
        {
            return (null, ExitCodes.FailedToOpenDevice);
        }

        Log.Info("Opened MIDI device " + linnstrument.Name);

      
        linnstrument.Initialize(25, 8);

        if (!linnstrument.TryApplyUserFirmwareMode(true))
        {
            Log.Error("Failed to apply user firmware mode");
            linnstrument.Dispose();
            return (null, ExitCodes.FailedToApplyUserFirmwareMode);
        }
        
        linnstrument.RequestAxes(LinnstrumentAxis.All);

        return (linnstrument: linnstrument, ExitCodes.Success);
    }
}

public enum ExitCodes
{
    Success = 0,
    FailedToOpenDevice = 1,
    FailedToApplyUserFirmwareMode = 2
}