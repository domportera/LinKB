using System.Diagnostics.CodeAnalysis;

namespace InputHooks;

[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum Layer
{
    Layer1 = 0,
    Layer2 = 1 << 0,
    Layer3 = 1 << 1,
    Layer4 = 1 << 2,
    Layer5 = Layer2 | Layer3,
    Layer6 = Layer2 | Layer4,
    Layer7 = Layer3 | Layer4,
    Layer8 = Layer2 | Layer3 | Layer4,
}

public static class LayerExtensions
{
    public const Layer Max = Layer.Layer8;
    public const int Count = (int)Max + 1;
    public static bool Contains(this Layer layer, Layer other) => (layer & other) == other;
}