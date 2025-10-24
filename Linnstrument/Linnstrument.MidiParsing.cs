using System.Diagnostics.CodeAnalysis;
using Midi.Net;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Linn;

public partial class Linnstrument
{
    private sbyte[,] _xAxisValuesMsb = null!;

    public bool TryParseMidiEvent(MidiEvent e, out PadStatusEvent? padStatusEvent, [NotNullWhen(false)] out string? reason)
    {
        var row = e.Status.Channel;

        switch (e.Status.Type)
        {
            case StatusType.NoteOn:
            {
                var column = e.Data.B1 - 1;
                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Velocity, e.Data.B2);
                reason = null;
                return true;
            }
            case StatusType.NoteOff:
            {
                var column = e.Data.B1 - 1;
                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Velocity, 0);
                reason = null;
                return true;
            }
            case StatusType.PolyKeyPressure:
            {
                // Z notes 0-25 = columns, 0-127 
                //var polyphonicPressure = new PolyphonicPressureMessage(e.Data.B1, e.Data.B2);
                var column = e.Data.B1 - 1;
                padStatusEvent = new PadStatusEvent(column, row, PadAxis.Z, e.Data.B2);
                reason = null;
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
                        if (!ValidatePosition(row, column, out reason))
                        {
                            padStatusEvent = null;
                            return false;
                        }
                        
                        _xAxisValuesMsb[column, row] = (sbyte)controlMessage.Value;
                        padStatusEvent = null;
                        reason = null;
                        return true;
                    }
                    case >= 32 and <= 57:
                    {
                        // X 14-bit LSB
                        const ushort maxReceivedValue = 4265;
                        
                        var column = controlMessage.CCNumber - 32 - 1;
                        if (!ValidatePosition(row, column, out reason))
                        {
                            padStatusEvent = null;
                            return false;
                        }

                        var perColumn = maxReceivedValue / _width;
                        ref var msb = ref _xAxisValuesMsb[column, row];
                        if (msb == Uninitialized)
                        {
                            padStatusEvent = null;
                            reason = "MSB for X axis not yet received";
                            return true;
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
                        reason = null;
                        return true;
                    }
                    default:
                    {
                        padStatusEvent = null;
                        reason = null;
                        return true;
                    }
                }
            }
            case StatusType.ChannelPressure:
            case StatusType.PitchBend:
            case StatusType.ProgramChange:
            default:
                padStatusEvent = null;
                reason = null;
                return true;
        }

        bool ValidatePosition(int rowY, int columnX, [NotNullWhen(false)] out string? reason)
        {
            if (rowY >= _height)
            {
                reason = $"Row {rowY} is out of bounds of height {_height}";
                return false;
            }

            if (columnX >= _width)
            {
                reason = $"Column {columnX} is out of bounds of width {_width}";
                return false;
            }

            reason = null;
            return true;
        }
    }
}