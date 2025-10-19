using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
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

    [Pure]
    public static string Serialize(ReadOnlySpan3D<KeyCode> config)
    {
        var sb = new System.Text.StringBuilder();

        for (int y = config.YLength - 1; y >= 0; y--)
        {
            for (int x = 0; x < config.XLength; x++)
            {
                for (int z = 0; z < config.ZLength; z++)
                {
                    ref readonly var key = ref config[x, y, z];
                    var keyStr = KeyInfo.ToName[key];
                    sb.Append(keyStr);
                    sb.Append(z < config.ZLength - 1 ? ModSeparator : '\t');
                }

                sb.Append(KeySeparator);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static bool TryDeserialize(string layoutStr, [NotNullWhen(true)] out KeyCode[,,]? keymap,
        [NotNullWhen(false)] out string? reason)
    {
        var lineSeparators = new[] { "\r\n", "\n" };

        var lines = layoutStr.Split(lineSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            keymap = null;
            reason = "No lines found in layout string";
            return false;
        }

        Array.Reverse(lines);

        var height = lines.Length;
        var width = lines[0].Split(KeySeparator, StringSplitOptions.RemoveEmptyEntries).Length;

        if (width == 0)
        {
            keymap = null;
            reason = "No keys found in first line - empty layout?";
            return false;
        }

        keymap =
            new KeyCode[width, height,
                LayerExtensions.Count]; // 3 layers for modifier combinations todo: make dynamic layer count

        for (int y = 0; y < height; y++)
        {
            var keys = lines[y].Split(KeySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (keys.Length != width)
            {
                keymap = null;
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
                        keymap = null;
                        reason = $"Unknown key name: {k} at position ({x},{y})";
                        code = KeyCode.Undefined;
                    }

                    keymap[x, y, index] = code;
                }
            }
        }

        keymap = (keymap);
        reason = null;
        return true;
    }

    private const int DeviceDepth = 8, DeviceWidth = 8;

    // todo - move to property extenions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<T> AsSpan<T>(Vector<T> v) => new(&v, Vector<T>.Count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<T2> AsSpan<T, T2>(this Vector<T> v) => new(&v, Vector<T>.Count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector<T> AsVector<T>(this Span<T> span) => new(span);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T X<T>(this Vector<T> v) => v[0];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Y<T>(this Vector<T> v) => v[1];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Z<T>(this Vector<T> v) => v[2];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T W<T>(this Vector<T> v) => v[3];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T R<T>(this Vector<T> v) => v[0];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T G<T>(this Vector<T> v) => v[1];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T B<T>(this Vector<T> v) => v[2];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T A<T>(this Vector<T> v) => v[3];
    
    
    
    public static async Task<KeyCode[,,]> LoadOrCreateKeymap(string preferencesFilePath, int width, int height, int depth)
    {
        if (File.Exists(preferencesFilePath))
        {
            var text = await File.ReadAllTextAsync(preferencesFilePath);
            if (TryDeserialize(text, out var keymap, out var reason))
            {
                (int xLength, int yLength, int zLength) = (keymap.GetLength(0), keymap.GetLength(1), keymap.GetLength(2));

                var span = new ReadOnlySpan3D<KeyCode>(keymap);
                if (xLength != span.XLength || yLength != span.YLength || zLength != span.ZLength)
                {
                    // save backup of current file
                    var fileName = Path.GetFileNameWithoutExtension(preferencesFilePath);
                    fileName += $"_deviceMismatchBackup_{xLength}x{yLength}x{zLength}.txt";
                    await File.WriteAllTextAsync(fileName, text);
                    
                    // resize the keymap to our desired width - a simple truncate //todo later: compress blank columns or rows to re-fit to smaller device
                    keymap = new KeyCode[width, height, depth];
                    Vector<int> newKeyDimensions = new(
                    [
                        Math.Min(xLength, width),
                        Math.Min(yLength, height),
                        Math.Min(zLength, depth)
                    ]);

                    var newKeymap = new KeyCode[newKeyDimensions.X(), newKeyDimensions.Y(), newKeyDimensions.Z()];
                    
                    for(int x = 0; x < newKeyDimensions.X(); x++)
                    {
                        for(int y = 0; y < newKeyDimensions.Y(); y++)
                        {
                            for(int z = 0; z < newKeyDimensions.Z(); z++)
                            {
                                newKeymap[x, y, z] = keymap[x, y, z];
                            }
                        }
                    }

                    if (!Save(preferencesFilePath, newKeymap))
                    {
                        Log.Error("Could not save resized keymap");
                    }
                    
                    Log.Warn($"Resized keymap from {xLength}x{yLength}x{zLength} to {width}x{height}x{depth}");
                    return newKeymap;
                }
                
                return keymap;
            }

            Log.Error($"Could not read preferences file: {preferencesFilePath}:\n{reason}");
        }

        return new KeyCode[25, 8, 8];
    }

    [Pure]
    public static bool Save(string filePath, ReadOnlySpan3D<KeyCode> config)
    {
        var layoutStr = Serialize(config);
        Log.Info("Serialized layout:\n" + layoutStr);
        try
        {
            File.WriteAllText(filePath, layoutStr);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Could not write preferences file: " + ex);
            return false;
        }
    }
}