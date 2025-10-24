using Midi.Net;

namespace LinKb;

[Serializable]
public record KeyColors
{
    public const LedColor UnlitColor = LedColor.Off;
    public const LedColor LitColor = LedColor.White;
    public const LedColor SpecialLitColor = LedColor.Cyan;
    public const LedColor PressedColor = LedColor.Pink;
    public const LedColor LockedColor = LedColor.Red;
    public const LedColor ModKeyColor = LedColor.Green;
    public const LedColor SymbolColor = LedColor.Lime;
    public const LedColor NumberColor = LedColor.Orange;
    public const LedColor HomeKeyColor = LedColor.Blue;
}