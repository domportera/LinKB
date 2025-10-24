using System.Diagnostics.CodeAnalysis;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Linn;

public partial class Linnstrument
{
    private sbyte[,] _xAxisValuesMsb = null!;

    public bool TryParseMidiEvent(MidiEvent e, [NotNullWhen(true)] out PadStatusEvent? padStatusEvent)
    {
        var row = e.Status.Channel;

        switch (e.Status.Type)
        {
            case StatusType.NoteOn:
            {
                var column = e.Data.B1 - 1;
                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Velocity, e.Data.B2);
                return true;
            }
            case StatusType.NoteOff:
            {
                var column = e.Data.B1 - 1;

                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Velocity, 0);
                return true;
            }
            case StatusType.PolyKeyPressure:
            {
                // Z notes 0-25 = columns, 0-127 
                //var polyphonicPressure = new PolyphonicPressureMessage(e.Data.B1, e.Data.B2);
                var column = e.Data.B1 - 1;
                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Z, e.Data.B2);
                return true;
            }
            case StatusType.ControlChange:
            {
                var controlMessage = new ControlChangeMessage(e);
                switch (controlMessage.CCNumber)
                {
                    case <= 25:
                    {
                        // X 14 bit MSB
                        var column = controlMessage.CCNumber - 1;

                        _xAxisValuesMsb[column, row] = (sbyte)controlMessage.Value;
                        padStatusEvent = null;
                        return false;
                    }
                    case >= 32 and <= 57:
                    {
                        // X 14-bit LSB
                        const ushort maxReceivedValue = 4265;
                        var perColumn = maxReceivedValue / _width;
                        var column = controlMessage.CCNumber - 32 - 1;

                        ref var msb = ref _xAxisValuesMsb[column, row];
                        if (msb == Uninitialized)
                        {
                            padStatusEvent = null;
                            return false;
                        }

                        var num = MidiParser.Value14Bit((byte)msb, controlMessage.Value);
                        msb = Uninitialized;

                        // convert to our column, as the X value received is a value from end-to-end of the X grid
                        //var derivedColumn = num / perColumn;
                        var cellValue = num % perColumn;
                        var derivedValue = cellValue / (float)perColumn;
                        padStatusEvent = new PadStatusEvent(column, row, PadAxis.X, derivedValue);
                        return true;
                    }
                    case >= 64 and <= 89:
                    {
                        // y is 7 bit cc 64-89
                        var column = controlMessage.CCNumber - 64 - 1;
                        padStatusEvent = new PadStatusEvent(column, row, PadAxis.Y, controlMessage.Value);
                        return true;
                    }
                    default:
                    {
                        padStatusEvent = null;
                        return false;
                    }
                }
            }
            case StatusType.ChannelPressure:
            case StatusType.PitchBend:
            case StatusType.ProgramChange:
            default:
                padStatusEvent = null;
                return false;
        }
    }
}