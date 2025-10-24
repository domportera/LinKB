using System.Numerics;
using Midi.Net.MidiUtilityStructs;

namespace LinKb.Core;

public static class PadStatusExtensions
{
    public static readonly Vector3 DefaultAxisValue = new(float.MinValue, float.MinValue, float.MinValue);

    public static void InitializeAsDefault(this PadStatus[,] pads)
    {
        var width = pads.GetLength(0);
        var height = pads.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pads[x, y] = new PadStatus
                {
                    Axes = DefaultAxisValue,
                };
            }
        }
    }

    public static void InitializeAsDefault(this Span<PadStatus> pads)
    {
        for (int i = 0; i < pads.Length; i++)
        {
            pads[i] = new PadStatus
            {
                Axes = DefaultAxisValue,
            };
        }
    }

    internal static void ApplyTo(this PadStatusEvent evt, ref PadStatus status)
    {
        if (evt.Axis == PadAxis.Velocity)
        {
            status.Velocity01 = evt.RawValueAbsolute;
            if (!status.IsPressed)
            {
                // clear its axes
                status.Axes = DefaultAxisValue;
            }
        }
        else
        {
            status.Axes[(int)evt.Axis] = evt.RawValueAbsolute;
        }
    }
}