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

        var keyHandler = new KeyHandler(items.InputEventProvider, items.Simulator);
        keyHandler.ApplyAutoRepeatSettings(items.AutoRepeatDelay, items.AutoRepeatRate);
        
        var grid = new MidiKeyboardGrid(gridDevice!, config, keyHandler);

        application.Initialize(items.InputEventProvider, grid);
        await Daemon.Run(grid, keyHandler, application);
        grid.Dispose();

        await KeySupport.End();
        await gridDevice!.CloseAsync();
        
        Log.Info("Application stopped");

        return ExitCodes.Success;
    }


    private static async Task<(MidiDevice? linnstrument, ExitCodes failedToOpenDevice)> TryOpenLinnstrument()
    {
        const string deviceSearchTerm = "linnstrument";
        var result = await DeviceHandler.TryOpen<Linnstrument>(deviceSearchTerm);
        
        if (!result.IsSuccess)
        {
            Log.Info("Failed to open MIDI device: " + result);
            return (null, ExitCodes.FailedToOpenDevice);
        }

        /Log.Info("Opened MIDI device " + linnstrument.Name);


        //linnstrument.Initialize(25, 8);
        
        //linnstrument.RequestAxes(LinnstrumentAxis.All);

        //return (linnstrument: linnstrument, ExitCodes.Success);
        return default;
    }
}

public enum ExitCodes
{
    Success = 0,
    FailedToOpenDevice = 1,
    FailedToApplyUserFirmwareMode = 2
}