using Midi.Net;

namespace KeyboardGUI.Keys;

[Serializable]
public record KeyColors
{
    public LedColor UnlitColor = LedColor.Off;
    public LedColor LitColor = LedColor.White;
    public LedColor SpecialLitColor = LedColor.Cyan;
    public LedColor PressedColor = LedColor.Pink;
    public LedColor LockedColor = LedColor.Red;
    public LedColor ModKeyColor = LedColor.Lime;
}