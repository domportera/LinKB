namespace LinKbGui;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            await LinKb.Main.Run(args, new GuiApplication());
        }
        catch (Exception ex)
        {
            await Console.Out.WriteAsync("Unhandled exception: " + ex);
        }
    }
}