using System.Numerics;
using InputHooks;
using Midi.Net;

namespace LinKb.Core;

public partial class MidiKeyboardGrid
{
    public Vector4 GetColorVector(int col, int row)
    {
        var color = GetColor(col, row, Layer);

        return color switch
        {
            LedColor.Off => Vector4.Zero,
            LedColor.Red => new Vector4(1f, 0f, 0f, 1f),
            LedColor.Blue => new Vector4(0f, 0f, 1f, 1f),
            LedColor.Green => new Vector4(0f, 1f, 0f, 1f),
            LedColor.Yellow => new Vector4(1f, 1f, 0f, 1f),
            LedColor.White => new Vector4(1f, 1f, 1f, 1f),
            LedColor.Cyan => new Vector4(0f, 1f, 1f, 1f),
            LedColor.Magenta => new Vector4(1f, 0f, 1f, 1f),
            LedColor.Default => new Vector4(0.5f, 0.5f, 0.5f, 1f),
            LedColor.Orange => new Vector4(1f, 0.5f, 0f, 1f),
            LedColor.Lime => new Vector4(0.75f, 1f, 0f, 1f),
            LedColor.Pink => new Vector4(1f, 0.75f, 0.8f, 1f),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private LedColor GetColor(int col, int row) => GetColor(col, row, Layer);

    private LedColor GetColor(int x, int y, Layer layer)
    {
        var keycode = _config.GetKey(x, y, layer, out _);
        if (keycode is KeyCode.Undefined or KeyCode.Blocker)
        {
            return KeyColors.UnlitColor;
        }

        if (_keyHandler.IsPressed(keycode))
        {
            return KeyColors.PressedColor;
        }

        if (keycode >= KeyCode.ModifierKeyMin) return KeyColors.ModKeyColor;

        if (keycode is KeyCode.F or KeyCode.J)
        {
            return KeyColors.HomeKeyColor;
        }

        if (keycode is KeyCode.CapsLock or KeyCode.NumLock or KeyCode.ScrollLock)
        {
            // todo - get actual lock status and color accordingly
            return KeyColors.LockedColor;
        }

        if (keycode.IsLetter())
        {
            return KeyColors.LitColor;
        }

        if (keycode.IsNumber())
            return KeyColors.NumberColor;

        return keycode.IsSymbol() ? KeyColors.SymbolColor : KeyColors.SpecialLitColor;
    }
}