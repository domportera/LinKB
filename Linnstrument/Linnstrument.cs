using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Linn;

public class Linnstrument : MidiDevice, ILEDGrid
{
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
                Value: color == LedColor.Off
                    ? (byte)7
                    : (byte)color) // color
        );
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