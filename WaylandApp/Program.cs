

using KeyboardGUI.Wayland;
using SharpHook.Data;

namespace WaylandApp;

public static class Program
{
    public static void Main()
    {
        KeycodeMap.Parse();
        return;
        var input = new WaylandInput();
        input.Init();
        List<WaylandInput.input_event> events = new List<WaylandInput.input_event>();

        var eventCount = 0;
        while (eventCount < 100)
        {
            events.Clear();
            input.ReadKeyboardEvents(events);
            eventCount += events.Count;

            if (events.Count <= 0) continue;
            
            foreach (var ev in events)
            {
                Console.WriteLine($"Key event: code={ev.code}, value={ev.value}");
            }

        }
        
        Console.WriteLine("Done");
        input.InjectKeyEvent(KeyCode.VcLeftMeta, 1);
        input.InjectKeyEvent(KeyCode.VcLeftMeta, 0);
        input.Dispose();
    }
}