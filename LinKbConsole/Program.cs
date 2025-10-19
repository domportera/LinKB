
// classic Program class

using KeyboardGUI.GUI;

public static class Program
{
    static async Task Main(string[] args)
    {
        await LinKb.Main.Run(args, new ConsoleApplication());
    }
}
