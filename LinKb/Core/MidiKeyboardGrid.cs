using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using InputHooks;
using LinKb.Configuration;
using Midi.Net;

namespace LinKb.Core;

public partial class MidiKeyboardGrid
{
    #region Public

    public int Width => _config.Width;
    public int Height => _config.Height;
    public Layer Layer => _layer;

    public bool EnableKeyEvents = true;
    public bool IsPadPressed(int x, int y) => _pads[x, y].IsPressed;
    public bool IsKeyPressed(KeyCode key) => _keyHandler.IsPressed(key);
    public KeyCode GetKey(int x, int y, out Layer foundLayer, Layer? layer = null) =>
        _config.GetKey(x, y, layer ?? Layer, out foundLayer);

    public Vector3 GetAxes(int col, int row) => _pads[col, row].Axes;

    public ReadOnlySpan3D<KeyCode> Keymap => _config.Keymap;

    public bool TrySetKey(int col, int row, Layer layer, KeyCode key, [NotNullWhen(false)] out string? reason)
    {
        if (_config.SetKey(col, row, layer, key, out reason))
        {
            UpdateLED(col, row);
            return true;
        }

        return false;
    }

    public void ApplyKeymap(ReadOnlySpan3D<KeyCode> loaded)
    {
        _config.SetKeymap(0, loaded);
        _ledHandler?.UpdateAndPushAll(_config.Width, _config.Height);
    }

    private void UpdateLED(int col, int row)
    {
        if (_ledHandler == null)
            return;

        _ledHandler.UpdateButtonState(col, row);
        _ledHandler.PushLEDs();
    }

    #endregion Public

    internal Span3D<KeyCode> KeymapRW => _config.KeymapRW;
    private readonly KeyHandler _keyHandler;
    public IReadOnlyDictionary<KeyCode, bool> KeyStates => _keyHandler.KeyStates;


    internal MidiKeyboardGrid(IMidiDevice device, KeyboardGridConfig config, KeyHandler keyHandler)
    {
        _keyHandler = keyHandler;
        
        // todo - get device connection/reconnection delegates that return midi device instead 
        // of providing it via the constructor, that way we can maintain the same grid object throughout disconnects?
        // or, create a way to create new grids throughout the lifetime of the application
        if (device is not IGridController)
        {
            throw new ArgumentException("Midi device must implement IGridController");
        }

        _pads = new PadStatus[config.Width, config.Height];
        _pads.InitializeAsDefault();

        _config = config;
        _device = device;
        if (device is ILEDGrid ledGrid)
        {
            _ledHandler = new LEDHandler(ledGrid, GetColor);
            _ledHandler.UpdateAndPushAll(_config.Width, _config.Height);
        }

        BeginReceiveThread();
    }


    internal void Dispose()
    {
        Dispose(true);
    }

    private void OnPadPress(int x, int y, bool pressed)
    {
        if (!EnableKeyEvents)
        {
            if (_ledHandler != null)
            {
                _ledHandler.UpdateButtonState(x, y);
                _ledHandler.PushLEDs();
            }
            
            return;
        }

        // manage key state
        var keycode = _config.GetKey(x, y, Layer, out var foundLayer);
        
        //Log.Debug($"Key {(pressed ? "Pressed" : "Released")}: {KeyInfo.ToName[keycode]} at ({x},{y}) on {Layer} from {foundLayer}");
        _keyHandler.HandleKeyPress(keycode, pressed);

        if (keycode >= KeyCode.ModifierKeyMin)
        {
            UpdateLayers(_keyHandler, _ledHandler, ref _layer, _config);
        }

        if (_ledHandler is not null)
        {
            _ledHandler.UpdateButtonState(x, y);
            _ledHandler.PushLEDs();
        }
    }


    private static void UpdateLayers(KeyHandler keyHandler, LEDHandler? leds, ref Layer currentLayer, KeyboardGridConfig config)
    {
        // non-modifier keys
        var previousLayer = currentLayer;

        // get new modifier state
        // todo - more normal way to get the mod keys - whats the "standardized" way to do this?
        var mod1 = keyHandler.IsPressed(KeyCode.Mod1);
        var mod2 = keyHandler.IsPressed(KeyCode.Mod2);
        var mod3 = keyHandler.IsPressed(KeyCode.Mod3);
        var newLayer = Layer.Layer1;
        if (mod1)
            newLayer |= Layer.Layer2;
        if (mod2)
            newLayer |= Layer.Layer3;
        if (mod3)
            newLayer |= Layer.Layer4;

        if (previousLayer == newLayer)
        {
            return; // no change in layer
        }

        currentLayer = newLayer;

        Log.Debug("Layer changed to " + currentLayer);

        // release keys that have been switched via layer change
        // make sure to ignore the modifier keys themselves
        // also update LED
        var width = config.Width;
        var height = config.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var newLayerKey = config.GetKey(x, y, newLayer, out _);
                if (newLayer > previousLayer && newLayerKey == KeyCode.Undefined)
                { // no layers have a key for this pad
                    continue;
                }

                var prevLayerKey = config.GetKey(x, y, previousLayer, out _);
                if (prevLayerKey == newLayerKey) 
                {
                    // ignore the keys that match that of the new layer
                    continue;
                }

                if (keyHandler.IsPressed(prevLayerKey))
                {
                    // release the key
                    // todo: this is an ugly place to do this? or maybe not?
                    keyHandler.HandleKeyPress(prevLayerKey, false);
                    Log.Info($"Key {prevLayerKey} released due to layer change");
                }
                
                // update the LED state for this key
                leds?.UpdateButtonState(x, y);

            }
        }
    }


    private void Dispose(bool disposing)
    {
        _device.MidiDevice.MidiReceived -= OnMidiReceived;
        _cancellationTokenSource.CancelAsync().Wait();
        _eventWaitHandle.Dispose();
        if (disposing)
        {
            GC.SuppressFinalize(this);
        }
    }

    ~MidiKeyboardGrid()
    {
        Dispose(false);
    }

    private readonly PadStatus[,] _pads;

    private Layer _layer;
    private readonly LEDHandler? _ledHandler;
    private readonly KeyboardGridConfig _config;
    private readonly IMidiDevice _device;
}