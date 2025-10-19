using System.Diagnostics.CodeAnalysis;
using InputHooks;
using LinKb.Core;
using LinKb.Keys;

namespace LinKb.Configuration;

[Serializable]
public class KeyboardGridConfig
{
    // todo: serialize colors
    public readonly KeyColors Colors = new();

    public KeyboardGridConfig(int xCount, int yCount) : this(new KeyCode[xCount, yCount, LayerExtensions.Count])
    {
    }

    internal KeyboardGridConfig(KeyCode[,,] keymap)
    {
        Keymap = keymap;
        var depth = keymap.GetLength(2);
        if (depth < LayerExtensions.Count)
        {
            // recreate the array with the correct depth
            var newMap = new KeyCode[keymap.GetLength(0), keymap.GetLength(1), LayerExtensions.Count];
            Array.Copy(keymap, newMap, keymap.Length);
            Keymap = newMap;
            Log.Error($"Keymap depth must be at least ({LayerExtensions.Count}). It was ({depth}). " +
                      $"Recreated the map to account for this.", nameof(keymap));
        }

        if (depth != LayerExtensions.Count)
            Log.Error(
                $"Warning: Keymap depth ({depth}) is greater than expected ({LayerExtensions.Count}). Extra layers will be ignored.",
                nameof(keymap));
    }

    public KeyCode[,,] Keymap { get; private set; }
    public int Width => Keymap.GetLength(0);
    public int Height => Keymap.GetLength(1);
    public int LayerCount => Keymap.GetLength(2);

    public KeyCode GetKey(int colX, int rowY, Layer modLevel, out Layer foundLayer)
    {
        var startIndex = (int)modLevel;
        for (int i = startIndex; i >= 0; i--)
        {
            var layer = (Layer)i;
            if ((modLevel & layer) != layer)
            {
                continue;
            }

            var key = Keymap[colX, rowY, i];
            if (key != KeyCode.Undefined)
            {
                foundLayer = layer;
                return key;
            }
        }

        foundLayer = Layer.Layer1;
        return KeyCode.Undefined;
    }

    public bool SetKey(int col, int row, Layer layer, KeyCode key, [NotNullWhen(false)] out string? reason)
    {
        if (layer != Layer.Layer1)
        {
            if (key >= KeyExtensions.ModifierKeyMin)
            {
                reason = "Cannot place special keys on non-default layers";
                return false;
            }

            var currentKey = GetKey(col, row, layer - 1, out var foundLayer);
            if (foundLayer != layer && currentKey >= KeyExtensions.ModifierKeyMin)
            {
                reason = "Cannot place keys on non-default layers if a modifier key is present on a lower layer";
                return false;
            }
        }

        Keymap[col, row, (int)layer] = key;
        reason = null;
        return true;
    }

    public void SetKeymap(int i, KeyCode[,,] keymap)
    {
        if (i == 0)
        {
            Keymap = keymap;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(i), "Only one keymap supported currently");
        }
    }
}