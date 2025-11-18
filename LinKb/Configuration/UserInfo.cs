using System.Diagnostics.Contracts;
using System.Numerics;
using InputHooks;
using LinKb.Core;
using Midi.Net;

namespace LinKb.Configuration;

public static class UserInfo
{
    static UserInfo()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    private const string AppName = "LinKb";
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppName);
    private static readonly string GridLayoutDirectory = Path.Combine(ConfigDirectory, "InputLayouts");
    private static readonly string ReadOnlyTemplateDirectory = Path.Combine(ConfigDirectory, "ReadOnly");

    // todo - file paths versioning such that files can be migrated to new versions easily
    private enum DirectoryType
    {
        GridLayout,
        Template,
    }
    private readonly record struct DirectorySchemeVersion(int Version);

    public struct ConfigFileInfo
    {
        public required string Name { get; init; }
        public required DateTime CreationTime { get; init; }
        public required DateTime LastAccessTime { get; init; }
        public required DateTime LastWriteTime { get; init; }

        private string? _filePath;
        public string FilePath => _filePath ??= Path.Combine(ConfigDirectory, Name, Name + ".txt");
    }
    
    private static ConfigFileInfo[]? _configFiles;

    public static IList<ConfigFileInfo> GetConfigFiles(bool forceRefresh = false)
    {
        if (_configFiles is null || forceRefresh)
        {
            var directoryInfo = new DirectoryInfo(ConfigDirectory);
            var subdirectories = directoryInfo.GetDirectories();
            // one config file per subdirectory
            _configFiles = subdirectories.Select(x => new ConfigFileInfo
            {
                Name = x.Name,
                CreationTime = x.CreationTime,
                LastAccessTime = x.LastAccessTime,
                LastWriteTime = x.LastWriteTime
            }).ToArray();
            
        }
        
        return _configFiles;
    }

    public static async Task<Result<KeyboardGridConfig>> TryLoadConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            return ResultFactory.Fail<KeyboardGridConfig>($"Config file not found: {configFilePath}");
        }

        var text = await File.ReadAllTextAsync(configFilePath);
        if (!LayoutSerializer.TryDeserialize(text, out var keymap, out var reason))
        {
            return ResultFactory.Fail<KeyboardGridConfig>($"Could not read preferences file: {configFilePath}:\n{reason}");
        }
        
        string name = Path.GetFileNameWithoutExtension(configFilePath);
        (int xLength, int yLength, int zLength) = (keymap.GetLength(0), keymap.GetLength(1), keymap.GetLength(2));
        
        var deviceSize = DeviceDimensions;
        if (xLength != deviceSize.X || yLength != deviceSize.Y || zLength != deviceSize.Z)
        {
            // save backup of current file
            var fileName = Path.GetFileNameWithoutExtension(configFilePath);
            fileName += $"_deviceMismatchBackup_{xLength}x{yLength}x{zLength}.txt";
            await File.WriteAllTextAsync(fileName, text);

            // resize the keymap to our desired width - a simple truncate //todo later: compress blank columns or rows to re-fit to smaller device
            keymap = new KeyCode[deviceSize.X, deviceSize.Y, deviceSize.Z];
            Vector<int> newKeyDimensions = new(
            [
                Math.Min(xLength, deviceSize.X),
                Math.Min(yLength, deviceSize.Y),
                Math.Min(zLength, deviceSize.Z)
            ]);

            var newKeymap = new KeyCode[newKeyDimensions.X(), newKeyDimensions.Y(), newKeyDimensions.Z()];

            for (int x = 0; x < newKeyDimensions.X(); x++)
            {
                for (int y = 0; y < newKeyDimensions.Y(); y++)
                {
                    for (int z = 0; z < newKeyDimensions.Z(); z++)
                    {
                        newKeymap[x, y, z] = keymap[x, y, z];
                    }
                }
            }

            if (!Save(configFilePath, newKeymap))
            {
                Log.Error("Could not save resized keymap");
            }

            return ResultFactory.Success(new KeyboardGridConfig(name, newKeymap), $"Resized keymap from {xLength}x{yLength}x{zLength} to {deviceSize.X}x{deviceSize.Y}x{deviceSize.Z}");
        }

        return ResultFactory.Success(new KeyboardGridConfig(name, keymap));
    }
    
    
    [Pure]
    private static Result Save(string path, ReadOnlySpan3D<KeyCode> config)
    {
        var layoutStr = LayoutSerializer.Serialize(config);
        // todo - async, move logs
        try
        {
            File.WriteAllText(path, layoutStr);
            return new Result(true, null);
        }
        catch (Exception ex)
        {
            return new Result(false, $"Could not write preferences to file: {ex}");
        }
    }

    // todo - save this regularly 
    private const string DefaultConfigFileName = "Default";
    private static string UserDefinedDefaultConfigFileName = "Default";
    private static string GetConfigFilePath(string configName) => Path.Combine(ConfigDirectory, configName, configName + ".txt");
    private static string GetConfigFilePath(KeyboardGridConfig config) => GetConfigFilePath(config.Name);

    public static async Task<KeyboardGridConfig> LoadOrCreateDefaultKeyConfig()
    {
        var configFiles = GetConfigFiles(true);
        if (!configFiles.Any())
        {
            return GetNewDefaultKeyConfig();
        }

        if (configFiles.Count > 1)
        {
            // tod: actually refer to an externally - saved "default" setting
        }
        
        var config = await TryLoadConfig(configFiles[0].FilePath);
        if (config.Success)
        {
            return config.Value;
        }
        
        // todo - load from a set of default templates/layouts
        return GetNewDefaultKeyConfig();
    }

    private static KeyboardGridConfig GetNewDefaultKeyConfig()
    {
        return new KeyboardGridConfig(DefaultConfigFileName,
            new KeyCode[DeviceDimensions.X, DeviceDimensions.Y, DeviceDimensions.Z]);
    }

    // todo - encode preferred device dimensions into the config files
    private static readonly Dimension3D DeviceDimensions = new(25, 8, LayerExtensions.Count);
    private readonly record struct Dimension3D(int X, int Y, int Z);

    public static Result Save(KeyboardGridConfig currentConfig)
    {
        var path = GetConfigFilePath(currentConfig);
        return Save(path, currentConfig.Keymap);
    }

    public static Result SaveAs(string name, KeyboardGridConfig currentConfig)
    {
        // todo- check for naming conflicts / overwrites
        var path = GetConfigFilePath(name);
        return Save(path, currentConfig.Keymap);
    }
}