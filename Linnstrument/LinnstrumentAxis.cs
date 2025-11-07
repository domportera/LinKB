using System.Diagnostics.CodeAnalysis;

namespace Linn;

[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum LinnstrumentAxis
{
    None = 0,
    X = 1,
    Y = 1 << 1,
    Z = 1 << 2,
    XY = X | Y,
    XZ = X | Z,
    YZ = Y | Z,
    All = X | Y | Z
}