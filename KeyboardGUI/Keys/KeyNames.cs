using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using SharpHook.Data;

namespace KeyboardGUI.Keys;

internal class KeyNames : IReadOnlyDictionary<KeyCode, string>, IReadOnlyDictionary<string, KeyCode>
{
    public static readonly FrozenDictionary<KeyCode, string> KeyToName;
    public static readonly FrozenDictionary<string, KeyCode> NameToKey;

    static KeyNames()
    {
        var dict = Enum.GetValues<KeyCode>()
            .ToDictionary(k => k, k => k.ToString()[2..]); // remove the "Vc" prefix

        dict[KeyCode.VcUndefined] = "None";
        dict[KeyExtensions.Mod1] = nameof(KeyExtensions.Mod1);
        dict[KeyExtensions.Mod2] = nameof(KeyExtensions.Mod2);
        dict[KeyExtensions.Mod3] = nameof(KeyExtensions.Mod3);

        KeyToName = dict.ToFrozenDictionary();
        NameToKey = dict.ToDictionary(kv => kv.Value, kv => kv.Key).ToFrozenDictionary();
    }

    IEnumerator<KeyValuePair<string, KeyCode>> IEnumerable<KeyValuePair<string, KeyCode>>.GetEnumerator() =>
        NameToKey.GetEnumerator();

    IEnumerator<KeyValuePair<KeyCode, string>> IEnumerable<KeyValuePair<KeyCode, string>>.GetEnumerator() =>
        KeyToName.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)KeyToName).GetEnumerator();
    IEnumerable<string> IReadOnlyDictionary<string, KeyCode>.Keys => KeyToName.Values;
    IEnumerable<KeyCode> IReadOnlyDictionary<string, KeyCode>.Values => KeyToName.Keys;
    IEnumerable<KeyCode> IReadOnlyDictionary<KeyCode, string>.Keys => KeyToName.Keys;
    IEnumerable<string> IReadOnlyDictionary<KeyCode, string>.Values => KeyToName.Values;
    public int Count => NameToKey.Count;

    public bool ContainsKey(string key) => NameToKey.ContainsKey(key);
    public bool ContainsKey(KeyCode key) => KeyToName.ContainsKey(key);
    public bool TryGetValue(string key, out KeyCode value) => NameToKey.TryGetValue(key, out value);

    public bool TryGetValue(KeyCode key, [NotNullWhen(true)] out string? value) =>
        KeyToName.TryGetValue(key, out value);

    public KeyCode this[string key] => NameToKey[key];
    public string this[KeyCode key] => KeyToName[key];
}