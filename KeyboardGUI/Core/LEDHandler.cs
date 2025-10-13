using System.Runtime.CompilerServices;
using KeyboardGUI.Configuration;
using Midi.Net;

namespace KeyboardGUI.Core;

internal class LEDHandler
{
    private readonly ILEDGrid _ledGrid;
    private readonly GetColorDelegate _getColor;

    public LEDHandler(ILEDGrid grid, GetColorDelegate getColor)
    {
        _getColor = getColor;
        _ledGrid = grid;
    }

    public void UpdateAndPushAll(int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                UpdateButtonState(x, y);
            }
        }

        _ledGrid.PushLEDs();
    }

    public void PushLEDs() => _ledGrid.PushLEDs();

    // Set the LED color based on the button state
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateButtonState(int x, int y) => _ledGrid.CommitLED(x, y, _getColor(x, y));
}

internal delegate bool ShouldLightDelegate(int x, int y, Layer depth);

internal delegate LedColor GetColorDelegate(int x, int y);