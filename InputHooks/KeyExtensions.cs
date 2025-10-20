using System.Diagnostics;

namespace InputHooks;

public static class KeyExtensions
{
    
    // todo - extension property when .NET 10 stable is released
    public static string Name(this KeyCode keycode) => KeyInfo.ToName[keycode];
    
    public static bool IsLetter(this KeyCode keycode) => keycode is >= KeyCode.Q and <= KeyCode.M;
    public static bool IsNumber(this KeyCode keycode) => keycode is >= KeyCode.N0 and <= KeyCode.N9 or >= KeyCode.NumPad0 and <= KeyCode.NumPad9;
    public static bool IsNumpadNumber(this KeyCode keycode) => keycode is >= KeyCode.NumPad0 and <= KeyCode.NumPad9;

    public static bool IsSymbol(this KeyCode keycode)
    {
        return keycode is KeyCode.Equals or KeyCode.NumPadEquals or KeyCode.Minus or KeyCode.NumPadAsterisk or KeyCode.NumPadMinus or KeyCode.NumPadSeparator
            or KeyCode.NumPadForwardSlash or KeyCode.NumPadPeriod or KeyCode.NumPadPlus or KeyCode.Slash
            or KeyCode.Backslash or KeyCode.BackQuote or KeyCode.Comma or KeyCode.Period
            or KeyCode.Semicolon;
    }

    public static bool IsMod(KeyCode key) => key >= KeyCode.ModifierKeyMin;
}