using System.Diagnostics.CodeAnalysis;
using InputHooks;
using LinKb.Core;

namespace LinKb.Configuration;

/// <summary>
/// Serializes keyboard layout arrays to/from strings for storage in settings
/// the format being an ascii chart of the keyboard grid, with each key represented as a KeyCode string
/// </summary>
public static class LayoutSerializer
{
    private const char KeySeparator = '|';
    private const char ModSeparator = '%';

    public static string Serialize(KeyboardGridConfig config)
    {
        var sb = new System.Text.StringBuilder();
        var depth = config.LayerCount;

        for (int y = config.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < config.Width; x++)
            {
                for (int l = 0; l < depth; l++)
                {
                    var key = config.Keymap[x, y, l];
                    var keyStr = KeyInfo.ToName[key];
                    sb.Append(keyStr);
                    sb.Append(l < depth - 1 ? ModSeparator : '\t');
                }

                sb.Append(KeySeparator);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static bool TryDeserialize(string layoutStr, [NotNullWhen(true)] out KeyboardGridConfig? config,
        [NotNullWhen(false)] out string? reason)
    {
        var lineSeparators = new[] { "\r\n", "\n" };

        var lines = layoutStr.Split(lineSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            config = null;
            reason = "No lines found in layout string";
            return false;
        }

        Array.Reverse(lines);

        var height = lines.Length;
        var width = lines[0].Split(KeySeparator, StringSplitOptions.RemoveEmptyEntries).Length;

        if (width == 0)
        {
            config = null;
            reason = "No keys found in first line - empty layout?";
            return false;
        }

        var keymap =
            new KeyCode[width, height,
                LayerExtensions.Count]; // 3 layers for modifier combinations todo: make dynamic layer count

        for (int y = 0; y < height; y++)
        {
            var keys = lines[y].Split(KeySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (keys.Length != width)
            {
                config = null;
                reason = $"Inconsistent row length at line {y}: expected {width}, got {keys.Length}";
                return false; // inconsistent row length
            }

            for (int x = 0; x < width; x++)
            {
                var keyStr = keys[x].Trim();
                var possibleKeys = keyStr.Split(ModSeparator, StringSplitOptions.RemoveEmptyEntries);
                for (var index = 0; index < possibleKeys.Length; index++)
                {
                    ref var k = ref possibleKeys[index];
                    k = k.Trim();
                    if (string.IsNullOrWhiteSpace(k))
                        continue;
                    if (!KeyInfo.ToKey.TryGetValue(k, out var code))
                    {
                        config = null;
                        reason = $"Unknown key name: {k} at position ({x},{y})";
                        code = KeyCode.Undefined;
                    }

                    keymap[x, y, index] = code;
                }
            }
        }

        config = new KeyboardGridConfig(keymap);
        reason = null;
        return true;
    }

    public static async Task<KeyboardGridConfig> LoadOrCreateConfig(string preferencesFilePath)
    {
        if (File.Exists(preferencesFilePath))
        {
            var text = await File.ReadAllTextAsync(preferencesFilePath);
            if (TryDeserialize(text, out var config, out var reason))
            {
                return config;
            }

            Log.Error($"Could not read preferences file: {preferencesFilePath}:\n{reason}");
        }

        return new KeyboardGridConfig(25, 8);
    }

    public static void Save(string filePath, KeyboardGridConfig config)
    {
        var layoutStr = LayoutSerializer.Serialize(config);
        Log.Info("Serialized layout:\n" + layoutStr);
        try
        {
            File.WriteAllText(filePath, layoutStr);
        }
        catch (Exception ex)
        {
            Log.Error("Could not write preferences file: " + ex);
        }
    }
}