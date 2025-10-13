namespace KeyboardGUI.Configuration;

internal static class UserInfo
{
    static UserInfo()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    public static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LinnstrumentKeyboard");

    public static readonly string DefaultConfigFile = Path.Combine(ConfigDirectory, "Preferences.txt");
}