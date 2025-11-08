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

        // 3 layers for modifier combinations todo: make dynamic layer count
        keymap = new KeyCode[width, height, LayerExtensions.Count]; 

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
                        reason = $"Unknown key name: {k} at position ({x},{y})";
                        code = KeyCode.Undefined;
                    }

                    keymap[x, y, index] = code;
                }
            }
        }

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
    
}