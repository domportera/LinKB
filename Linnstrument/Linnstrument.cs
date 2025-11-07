using Commons.Music.Midi;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Linn;

public partial class Linnstrument : IMidiDevice, ILEDGrid, IGridController
{
    public int Width { get; private set; }

    public int Height { get; private set; }
    public event EventHandler? ConnectionStateChanged;

    private bool _inUserFirmwareMode;
    private const sbyte Uninitialized = sbyte.MinValue;
    public MidiDevice MidiDevice { get; init; }

    public async Task<Result> OnConnect()
    {
        var error = "";
        try
        {
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            error += $"Error invoking ConnectionStateChanged event: {ex.Message}";
        }

        if (!await TryApplyUserFirmwareMode(true))
        {
            error += "Failed to apply user firmware mode";
        }
        
        // todo: load Linnstrument-specific config to get the width, height, axes, etc
        const int width = 25, height = 8;
        Width = width;
        Height = height;
        _xAxisValuesMsb = new sbyte[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _xAxisValuesMsb[x, y] = Uninitialized;
            }
        }
        
        RequestAxes(LinnstrumentAxis.All);
        return new(true, error);
    }

    public async Task<Result> CloseAsync()
    {
        var stoppedUserFirmwareMode = await TryApplyUserFirmwareMode(false);
        await (MidiDevice as IMidiPort).CloseAsync();
        
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        if(stoppedUserFirmwareMode)
        {
            return new Result(true, null);
        }
        
        return new Result(false, "Failed to unset user firmware mode");
    }


    /// <summary>
    /// <a href="https://github.com/rogerlinndesign/linnstrument-firmware/blob/master/user_firmware_mode.txt">
    /// See documentation</a>
    /// and the implementation spec of <a href="https://en.wikipedia.org/wiki/NRPN">NRPN</a>
    /// </summary>
    private async Task<bool> TryApplyUserFirmwareMode(bool on)
    {
        // User Mode can be activated by sending LinnStrument the value 1 for
        // MIDI NRPN 245 on any MIDI channel, sending value 0 will turn it off
        MidiDevice.CommitNrpn(245, on ? 1 : 0, 0);
        MidiDevice.PushMidi();
        await Task.Delay(500);
        _inUserFirmwareMode = on;
        // todo - await acknowledgement
        return true;
    }

    public void CommitLED(int x, int y, LedColor color)
    {
        // Set the LED of a specific pad at (x, y) to the given color
        MidiDevice.CommitCC(channel: 0,
            new ControlChangeMessage((ControlChange)20, (byte)(x + 1)), // column
            new ControlChangeMessage((ControlChange)21, (byte)y), // row
            new ControlChangeMessage((ControlChange)22,
                value: color == LedColor.Off
                    ? (byte)7
                    : (byte)color) // color
        );
    }

    private void RequestAxes(LinnstrumentAxis linnstrumentAxes, int row = -1, Channel channel = Channel.Channel1To8)
    {
        if (row != -1)
            throw new NotImplementedException();

        for(int a = 0; a < 3; a++)
        {
            var axis = (LinnstrumentAxis)(1 << a);
            var enabled = (linnstrumentAxes & axis) != 0;
            var value = (byte)(enabled ? 1 : 0);
            var cc = (ControlChange)(10 + a);
            for (int c = 0; c < 16; c++)
            {
                var currentChannel = (Channel)(1 << c);
                var channelIsActive = (currentChannel & channel) != 0;
                if (channelIsActive)
                {
                    MidiDevice.CommitCC(c, new ControlChangeMessage(cc, value));
                }
            }
        }
        
        MidiDevice.PushMidi();
    }

    void ILEDGrid.PushLEDs() => MidiDevice.PushMidi();

    // notes:
    // NRPN 245    Enabling/disabling User Firmware mode (0: disable, 1: enable)
    // CC 9        Configure User Firmware X-axis row slide, the channel specifies the row (0: disable, 1: enable)
    // CC 10       Configure User Firmware X-axis data, the channel specifies the row, default is off (0: disable, 1: enable)
    // CC 11       Configure User Firmware Y-axis data, the channel specifies the row, default is off (0: disable, 1: enable)
    // CC 12       Configure User Firmware Z-axis data, the channel specifies the row, default is off (0: disable, 1: enable)
    // CC 13       Configure User Firmware MIDI decimation rate in milliseconds (minimum 12 ms in low power mode)
    // CC 20       Column coordinate for cell color change with CC 22 (starts from 0)
    //     CC 21       Row coordinate for cell color change with CC 22 (starts from 0)
    //     CC 22       Change the color of the cell with the provided column and row coordinates
    //     see color value table in midi.txt, 7+: default color
}