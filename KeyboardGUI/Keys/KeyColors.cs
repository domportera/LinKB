using Midi.Net;

namespace KeyboardGUI.Keys;

[Serializable]
public record KeyColors
{
    public const LedColor UnlitColor = LedColor.Off;
    public const LedColor LitColor = LedColor.White;
    public const LedColor SpecialLitColor = LedColor.Cyan;
    public const LedColor PressedColor = LedColor.Pink;
    public const LedColor LockedColor = LedColor.Red;
    public const LedColor ModKeyColor = LedColor.Lime;
    public const LedColor NumberSymbolColor = LedColor.Green;
    public const LedColor HomeKeyColor = LedColor.Blue;
}