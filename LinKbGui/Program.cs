namespace LinKbGui;

class Program
{
    static async Task Main(string[] args)
    {
        await LinKb.Main.Run(args, new GuiApplication());
    }
}