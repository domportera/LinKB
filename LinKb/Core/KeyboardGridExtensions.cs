using System.Diagnostics.CodeAnalysis;
using InputHooks;

namespace LinKb.Core;

public static class KeyboardGridExtensions
{
    public static bool SwapWholeLayers(this MidiKeyboardGrid grid, Layer aLayer, Layer bLayer, [NotNullWhen(false)] out string? reason)
    {
        if (aLayer == bLayer)
        {
            reason = null;
            return true;
        }

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                grid.KeymapRW[x, y, (int)aLayer] = grid.KeymapRW[x, y, (int)bLayer];
            }
        }

        reason = null;
        return true;
    }
    
    public static bool SwapKeyLayer(this MidiKeyboardGrid grid, Layer aLayer, Layer bLayer, int xColumn, int yRow, [NotNullWhen(false)] out string? reason)
    {
        if (aLayer == bLayer)
        {
            reason = null;
            return true;
        }

        var aKey = grid.GetKey(xColumn, yRow, out var foundLayerA, aLayer);
        var bKey = grid.GetKey(xColumn, yRow, out var foundLayerB, bLayer);

        if (foundLayerA != aLayer || foundLayerB != bLayer)
        {
            reason = "Cannot swap layers when one of the layers does not have a key assigned at the specified position";
            return false;
        }
        
        if(!grid.TrySetKey(xColumn, yRow, bLayer, aKey, out reason))
        {
            return false;
        }
        
        if(!grid.TrySetKey(xColumn, yRow, aLayer, bKey, out reason))
        {
            // revert
            grid.TrySetKey(xColumn, yRow, bLayer, bKey, out _);
            return false;
        }

        return true;
    }
    
}