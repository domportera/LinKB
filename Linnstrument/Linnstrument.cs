using System.Diagnostics.CodeAnalysis;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Linn;

public partial class Linnstrument : MidiDevice, ILEDGrid, IGridController
{
    private int _width, _height;
    private const sbyte Uninitialized = sbyte.MinValue;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _xAxisValuesMsb = new sbyte[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _xAxisValuesMsb[x, y] = Uninitialized;
            }
        }
    }

    protected override async Task<(bool Success, string? Error)> OnClose()
    {
        var success = TryApplyUserFirmwareMode(false);
        if (success)
        {
            await Task.Delay(200); // dummy wait since we're not actually waiting for device response yet
            return (true, null);
        }

        return (false, "Failed to unset user firmware mode");
    }
    
    /// <summary>
    /// <a href="https://github.com/rogerlinndesign/linnstrument-firmware/blob/master/user_firmware_mode.txt">
    /// See documentation</a>
    /// and the implementation spec of <a href="https://en.wikipedia.org/wiki/NRPN">NRPN</a>
    /// </summary>
    public bool TryApplyUserFirmwareMode(bool on)
    {
        // User Mode can be activated by sending LinnStrument the value 1 for
        // MIDI NRPN 245 on any MIDI channel, sending value 0 will turn it off
        CommitNrpn(245, on ? 1 : 0, 0);
        PushMidi();
        // todo - await acknowledgement
        return true;
    }

    public void CommitLED(int x, int y, LedColor color)
    {
        // Set the LED of a specific pad at (x, y) to the given color
        CommitCC(channel: 0,
            new ControlChangeMessage((ControlChange)20, (byte)(x + 1)), // column
            new ControlChangeMessage((ControlChange)21, (byte)y), // row
            new ControlChangeMessage((ControlChange)22,
                value: color == LedColor.Off
                    ? (byte)7
                    : (byte)color) // color
        );
    }

    public void RequestAxes(LinnstrumentAxis linnstrumentAxes, int row = -1, Channel channel = Channel.Channel1To8)
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
                    CommitCC(c, new ControlChangeMessage(cc, value));
                }
            }
        }
        
        PushMidi();
    }

    void ILEDGrid.PushLEDs() => PushMidi();

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