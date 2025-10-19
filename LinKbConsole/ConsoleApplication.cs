using InputHooks;
using LinKb.Application;
using LinKb.Core;

namespace KeyboardGUI.GUI;

internal class ConsoleApplication : IApplication
{
    private IEventProvider? _hooks;
    private MidiKeyboardGrid? _grid;

    public void Initialize(IEventProvider hooks, MidiKeyboardGrid grid)
    {
        _hooks = hooks;
        _grid = grid;
    }

    public async Task Run()
    {
        if (_hooks is null || _grid is null)
            throw new InvalidOperationException("Application not initialized");

        await Task.Run(() =>
        {
            Log.Info("LinKB application started - press Ctrl+C to exit.");
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                exitEvent.Set();
            };

            while (true)
            {
                if (exitEvent.WaitOne(100))
                {
                    break;
                }

                DrawGridKeysInConsole(_grid);
            }

            exitEvent.WaitOne();
        });
    }

    /// <summary>
    /// This method draws the all 25x8 grid keys in the console, adjusting for the console size.
    /// The names of each key are drawn in each grid cell.
    /// </summary>
    /// <param name="grid"></param>
    private void DrawGridKeysInConsole(MidiKeyboardGrid grid)
    {
        var consoleWidth = Console.WindowWidth;
        for (int x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                var key = grid.GetKey(x, y, out var layer);
                var color = LayerColors[(int)layer];
                int posX, posY;
                Console.ForegroundColor = color;
                Console.Write(key.ToString());
                Console.ResetColor();
                Console.Write(" ");
            }
        }
    }
    
    private static readonly ConsoleColor[] LayerColors = Enum.GetValues<ConsoleColor>();
}