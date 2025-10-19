using System.Collections.Frozen;
using SharpHook.Data;

using KC = InputHooks.KeyCode;

namespace SharphookInput;

internal static class KeycodeMap
{
    public static readonly IReadOnlyDictionary<KeyCode, KC> HooksCodeToKeyCode;
    public static readonly IReadOnlyDictionary<KC, KeyCode> KeyCodeToHooksCode;
    
    public static readonly IReadOnlyDictionary<KC, MouseButton> KeyCodeToMouseButton =
        new Dictionary<KC, MouseButton>
        {
            { KC.MouseLeft, MouseButton.Button1 },
            { KC.MouseRight, MouseButton.Button2 },
            { KC.MouseMiddle, MouseButton.Button3 },
            { KC.MouseBack, MouseButton.Button4 },
            { KC.MouseForward, MouseButton.Button5 }
        }.ToFrozenDictionary();

    static KeycodeMap()
    {
        (KeyCode SharpHook, KC InputHooks)[] map =
        [
            (KeyCode.VcUndefined, KC.Undefined),
            
            // alphanumeric keys
            (KeyCode.Vc0, KC.N0),
            (KeyCode.Vc1, KC.N1),
            (KeyCode.Vc2, KC.N2),
            (KeyCode.Vc3, KC.N3),
            (KeyCode.Vc4, KC.N4),
            (KeyCode.Vc5, KC.N5),
            (KeyCode.Vc6, KC.N6),
            (KeyCode.Vc7, KC.N7),
            (KeyCode.Vc8, KC.N8),
            (KeyCode.Vc9, KC.N9),
            (KeyCode.VcA, KC.A),
            (KeyCode.VcB, KC.B),
            (KeyCode.VcC, KC.C),
            (KeyCode.VcD, KC.D),
            (KeyCode.VcE, KC.E),
            (KeyCode.VcF, KC.F),
            (KeyCode.VcG, KC.G),
            (KeyCode.VcH, KC.H),
            (KeyCode.VcI, KC.I),
            (KeyCode.VcJ, KC.J),
            (KeyCode.VcK, KC.K),
            (KeyCode.VcL, KC.L),
            (KeyCode.VcM, KC.M),
            (KeyCode.VcN, KC.N),
            (KeyCode.VcO, KC.O),
            (KeyCode.VcP, KC.P),
            (KeyCode.VcQ, KC.Q),
            (KeyCode.VcR, KC.R),
            (KeyCode.VcS, KC.S),
            (KeyCode.VcT, KC.T),
            (KeyCode.VcU, KC.U),
            (KeyCode.VcV, KC.V),
            (KeyCode.VcW, KC.W),
            (KeyCode.VcX, KC.X),
            (KeyCode.VcY, KC.Y),
            (KeyCode.VcZ, KC.Z),
            
            
            // modifier keys
            (KeyCode.VcLeftShift, KC.LeftShift),
            (KeyCode.VcRightShift, KC.RightShift),
            (KeyCode.VcLeftControl, KC.LeftControl),
            (KeyCode.VcRightControl, KC.RightControl),
            (KeyCode.VcLeftAlt, KC.LeftAlt),
            (KeyCode.VcRightAlt, KC.RightAlt),
            (KeyCode.VcLeftMeta, KC.LeftMeta),
            (KeyCode.VcRightMeta, KC.RightMeta),
            (KeyCode.VcBackspace, KC.Backspace),
            
            // navigation keys
            (KeyCode.VcTab, KC.Tab),
            (KeyCode.VcEnter, KC.Enter),
            (KeyCode.VcEscape, KC.Escape),
            (KeyCode.VcHome, KC.Home),
            (KeyCode.VcEnd, KC.End),
            (KeyCode.VcPageUp, KC.PageUp),
            (KeyCode.VcPageDown, KC.PageDown),
            (KeyCode.VcLeft, KC.Left),
            (KeyCode.VcRight, KC.Right),
            (KeyCode.VcDown, KC.Down),
            (KeyCode.VcUp, KC.Up),
            
            // lock keys
            (KeyCode.VcNumLock, KC.NumLock),
            (KeyCode.VcCapsLock, KC.CapsLock),
            (KeyCode.VcScrollLock, KC.ScrollLock),
            
            (KeyCode.VcPause, KC.Pause),
            (KeyCode.VcInsert, KC.Insert),
            (KeyCode.VcDelete, KC.Delete),
            
            // standard punctuation
            (KeyCode.VcBackslash, KC.Backslash),
            (KeyCode.VcQuote, KC.Quote),
            (KeyCode.VcSemicolon, KC.Semicolon),
            (KeyCode.VcMinus, KC.Minus),
            (KeyCode.VcEquals, KC.Equals),
            (KeyCode.VcSlash, KC.Slash),
            (KeyCode.VcSpace, KC.Space),
            (KeyCode.VcComma, KC.Comma),
            (KeyCode.VcPeriod, KC.Period),
            (KeyCode.VcBackQuote, KC.BackQuote),
            (KeyCode.VcOpenBracket, KC.OpenBracket),
            (KeyCode.VcCloseBracket, KC.CloseBracket),
            (KeyCode.VcUnderscore, KC.Underscore),
            
            // apps
            (KeyCode.VcApp1, KC.App1),
            (KeyCode.VcApp2, KC.App2),
            (KeyCode.VcApp3, KC.App3),
            (KeyCode.VcApp4, KC.App4),
            (KeyCode.VcAppCalculator, KC.AppCalculator),
            (KeyCode.VcAppBrowser, KC.AppBrowser),
            (KeyCode.VcAppMail, KC.AppMail),
            
            // browser
            (KeyCode.VcBrowserBack, KC.BrowserBack),
            (KeyCode.VcBrowserForward, KC.BrowserForward),
            (KeyCode.VcBrowserRefresh, KC.BrowserRefresh),
            (KeyCode.VcBrowserStop, KC.BrowserStop),
            (KeyCode.VcBrowserSearch, KC.BrowserSearch),
            (KeyCode.VcBrowserFavorites, KC.BrowserFavorites),
            (KeyCode.VcBrowserHome, KC.BrowserHome),
            
            // media
            (KeyCode.VcMediaPlay, KC.MediaPlay),
            (KeyCode.VcMediaNext, KC.MediaNext),
            (KeyCode.VcMediaPrevious, KC.MediaPrevious),
            (KeyCode.VcMediaStop, KC.MediaStop),
            (KeyCode.VcMediaEject, KC.MediaEject),
            (KeyCode.VcMediaSelect, KC.MediaSelect),
            (KeyCode.VcVolumeDown, KC.VolumeDown),
            (KeyCode.VcVolumeUp, KC.VolumeUp),
            (KeyCode.VcVolumeMute, KC.VolumeMute),
            
            // language-specific
            (KeyCode.VcAlphanumeric, KC.AlphaNumeric),
            (KeyCode.VcHiragana, KC.Hiragana),
            (KeyCode.VcKatakana, KC.Katakana),
            (KeyCode.VcKatakanaHiragana, KC.KatakanaHiragana),
            (KeyCode.VcJpComma, KC.JpComma),
            (KeyCode.VcYen, KC.Yen),
            (KeyCode.VcKanji, KC.Kanji),
            (KeyCode.VcKana, KC.Kana),
            (KeyCode.VcJunja, KC.Junja),
            (KeyCode.VcHanja, KC.Hanja),
            (KeyCode.VcHangul, KC.Hangul),
            (KeyCode.VcConvert, KC.Convert),
            (KeyCode.VcNonConvert, KC.NonConvert),
            
            // System / uncommon
            (KeyCode.VcContextMenu, KC.ContextMenu),
            (KeyCode.VcPrintScreen, KC.PrintScreen),
            (KeyCode.VcChangeInputSource, KC.ChangeInputSource),
            (KeyCode.VcHelp, KC.Help),
            (KeyCode.VcFinal, KC.Final),
            (KeyCode.VcFunction, KC.Function),
            (KeyCode.VcAccept, KC.Accept),
            (KeyCode.VcCancel, KC.Cancel),
            (KeyCode.VcMisc, KC.VendorKey),
            (KeyCode.VcImeOn, KC.ImeOn),
            (KeyCode.VcImeOff, KC.ImeOff),
            (KeyCode.VcModeChange, KC.ModeChange),
            (KeyCode.VcSleep, KC.Sleep),
            (KeyCode.VcPower, KC.Power),
            (KeyCode.VcProcess, KC.Process),
            (KeyCode.Vc102, KC.Vc102),
            
            // numpad
            (KeyCode.VcNumPad0, KC.NumPad0),
            (KeyCode.VcNumPad1, KC.NumPad1),
            (KeyCode.VcNumPad2, KC.NumPad2),
            (KeyCode.VcNumPad3, KC.NumPad3),
            (KeyCode.VcNumPad4, KC.NumPad4),
            (KeyCode.VcNumPad5, KC.NumPad5),
            (KeyCode.VcNumPad6, KC.NumPad6),
            (KeyCode.VcNumPad7, KC.NumPad7),
            (KeyCode.VcNumPad8, KC.NumPad8),
            (KeyCode.VcNumPad9, KC.NumPad9),
            (KeyCode.VcNumPadMultiply, KC.NumPadAsterisk),
            (KeyCode.VcNumPadAdd, KC.NumPadPlus),
            (KeyCode.VcNumPadSubtract, KC.NumPadMinus),
            (KeyCode.VcNumPadDecimal, KC.NumPadPeriod),
            (KeyCode.VcNumPadDivide, KC.NumPadForwardSlash),
            (KeyCode.VcNumPadEnter, KC.NumPadEnter),
            (KeyCode.VcNumPadEquals, KC.NumPadEquals),
            (KeyCode.VcNumPadSeparator, KC.NumPadSeparator),
            (KeyCode.VcNumPadClear, KC.NumPadClear),
            
            // fn keys
            (KeyCode.VcF1, KC.F1),
            (KeyCode.VcF2, KC.F2),
            (KeyCode.VcF3, KC.F3),
            (KeyCode.VcF4, KC.F4),
            (KeyCode.VcF5, KC.F5),
            (KeyCode.VcF6, KC.F6),
            (KeyCode.VcF7, KC.F7),
            (KeyCode.VcF8, KC.F8),
            (KeyCode.VcF9, KC.F9),
            (KeyCode.VcF10, KC.F10),
            (KeyCode.VcF11, KC.F11),
            (KeyCode.VcF12, KC.F12),
            (KeyCode.VcF13, KC.F13),
            (KeyCode.VcF14, KC.F14),
            (KeyCode.VcF15, KC.F15),
            (KeyCode.VcF16, KC.F16),
            (KeyCode.VcF17, KC.F17),
            (KeyCode.VcF18, KC.F18),
            (KeyCode.VcF19, KC.F19),
            (KeyCode.VcF20, KC.F20),
            (KeyCode.VcF21, KC.F21),
            (KeyCode.VcF22, KC.F22),
            (KeyCode.VcF23, KC.F23),
            (KeyCode.VcF24, KC.F24)
        ];
        
        HooksCodeToKeyCode = map
            .ToDictionary(x => x.SharpHook, x => x.InputHooks)
            .ToFrozenDictionary();
        
        KeyCodeToHooksCode = map
            .ToDictionary(x => x.InputHooks, x => x.SharpHook)
            .ToFrozenDictionary();
    }
}