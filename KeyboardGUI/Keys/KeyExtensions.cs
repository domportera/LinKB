using System.Diagnostics;
using KeyboardGUI.Configuration;
using SharpHook.Data;

namespace KeyboardGUI.Keys;

internal static class KeyExtensions
{
    static KeyExtensions()
    {
        Debug.Assert(!KeyCode.VcUp.IsNormal());
        Debug.Assert(!KeyCode.VcDown.IsNormal());
        Debug.Assert(!KeyCode.VcLeft.IsNormal());
        Debug.Assert(!KeyCode.VcRight.IsNormal());
        Debug.Assert(!KeyCode.VcEnter.IsNormal());
        Debug.Assert(!KeyCode.VcBackspace.IsNormal());
        Debug.Assert(!KeyCode.VcDelete.IsNormal());
        Debug.Assert(!KeyCode.VcInsert.IsNormal());
        Debug.Assert(!KeyCode.VcHome.IsNormal());
        Debug.Assert(!KeyCode.VcEnd.IsNormal());
        Debug.Assert(!KeyCode.VcPageUp.IsNormal());
        Debug.Assert(!KeyCode.VcPageDown.IsNormal());
        Debug.Assert(!KeyCode.VcF1.IsNormal());
        Debug.Assert(!KeyCode.VcF12.IsNormal());
        Debug.Assert(!KeyCode.VcEnter.IsNormal());
        Debug.Assert(!KeyCode.VcBackspace.IsNormal());
        Debug.Assert(!KeyCode.VcLeftShift.IsNormal());
        Debug.Assert(!KeyCode.VcRightShift.IsNormal());
        Debug.Assert(!KeyCode.VcLeftControl.IsNormal());
        Debug.Assert(!KeyCode.VcRightControl.IsNormal());
        Debug.Assert(!KeyCode.VcLeftAlt.IsNormal());
        Debug.Assert(!KeyCode.VcRightAlt.IsNormal());
        Debug.Assert(!KeyCode.VcLeftMeta.IsNormal());
        Debug.Assert(!KeyCode.VcRightMeta.IsNormal());
        Debug.Assert(!KeyCode.VcCapsLock.IsNormal());
        Debug.Assert(!KeyCode.VcNumLock.IsNormal());
        Debug.Assert(!KeyCode.VcScrollLock.IsNormal());
        Debug.Assert(!KeyCode.VcPrintScreen.IsNormal());
        Debug.Assert(!KeyCode.VcPause.IsNormal());
        Debug.Assert(!KeyCode.VcContextMenu.IsNormal());
        Debug.Assert(!KeyCode.VcApp1.IsNormal());
        Debug.Assert(!KeyCode.VcApp2.IsNormal());
        Debug.Assert(!KeyCode.VcMediaPlay.IsNormal());
        Debug.Assert(!KeyCode.VcMediaStop.IsNormal());
        Debug.Assert(!KeyCode.VcMediaPrevious.IsNormal());
        Debug.Assert(!KeyCode.VcMediaNext.IsNormal());
        Debug.Assert(!KeyCode.VcMediaSelect.IsNormal());
        Debug.Assert(!KeyCode.VcMediaEject.IsNormal());
        Debug.Assert(!KeyCode.VcVolumeMute.IsNormal());
        Debug.Assert(!KeyCode.VcVolumeUp.IsNormal());
        Debug.Assert(!KeyCode.VcVolumeDown.IsNormal());
        Debug.Assert(!KeyCode.VcAppCalculator.IsNormal());
        Debug.Assert(!KeyCode.VcAppMail.IsNormal());
        Debug.Assert(!KeyCode.VcAppBrowser.IsNormal());
        Debug.Assert(!KeyCode.VcChangeInputSource.IsNormal());
        Debug.Assert(!KeyCode.VcKatakana.IsNormal());
        Debug.Assert(!KeyCode.VcHiragana.IsNormal());
        Debug.Assert(!KeyCode.VcKatakanaHiragana.IsNormal());
        Debug.Assert(!KeyCode.VcConvert.IsNormal());
        Debug.Assert(!KeyCode.VcNonConvert.IsNormal());


        // application-specific modifier keys
        Debug.Assert(!Mod1.IsNormal());
        Debug.Assert(!Mod2.IsNormal());
        Debug.Assert(!Mod3.IsNormal());
        Debug.Assert(!KeyCode.VcW.IsNormal());
        Debug.Assert(!KeyCode.VcA.IsNormal());
        Debug.Assert(!KeyCode.VcS.IsNormal());
        Debug.Assert(!KeyCode.VcD.IsNormal());

        // normal keys 
        Debug.Assert(KeyCode.VcZ.IsNormal());
        Debug.Assert(KeyCode.Vc0.IsNormal());
        Debug.Assert(KeyCode.Vc9.IsNormal());
        Debug.Assert(KeyCode.VcEscape.IsNormal());
    }

    public static bool IsNormal(this KeyCode keycode)
    {
        return (int)keycode <= 124 // VcJpComma  
               && keycode is > KeyCode.VcDown and <= KeyCode.VcNumPadDivide or KeyCode.VcNumPadEquals
                   or KeyCode.VcEscape

               // exclude WASD for gaming/production use cases
               && keycode is not KeyCode.VcW and not KeyCode.VcA and not KeyCode.VcS and not KeyCode.VcD;
    }

    public const KeyCode Mod1 = (KeyCode)ushort.MaxValue - (int)Layer.Layer2 + 1;
    public const KeyCode Mod2 = (KeyCode)ushort.MaxValue - (int)Layer.Layer3 + 1;
    public const KeyCode Mod3 = (KeyCode)ushort.MaxValue - (int)Layer.Layer4 + 1;
    public const KeyCode ModifierKeyMin = Mod3;
    public const KeyCode ModifierKeyMax = Mod1;
}