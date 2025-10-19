using System.Collections.Frozen;

namespace InputHooks;

public static class KeyInfo
{
    public static readonly FrozenDictionary<KeyCode, string> ToName;
    public static readonly FrozenDictionary<string, KeyCode> ToKey;
    public static readonly FrozenDictionary<KeyCode, string> MultilineNames;
    public static readonly IReadOnlyList<KeyValuePair<KeyCode, string>> OrderedKeys;
    public static readonly IReadOnlyList<KeyCode> AllKeys;

    static KeyInfo()
    {
        AllKeys = Enum.GetValues<KeyCode>().OrderBy(x => (int)x).ToArray();
        var dict = AllKeys
            .ToDictionary(k => k, k =>
            {
                var nameStr = k.ToString();
                if(nameStr.StartsWith("Vc"))
                    nameStr = nameStr[2..];
                return nameStr;
            }); // remove the "Vc" prefix

        dict[KeyCode.Undefined] = "None";
        dict[KeyCode.Mod1] = nameof(KeyCode.Mod1);
        dict[KeyCode.Mod2] = nameof(KeyCode.Mod2);
        dict[KeyCode.Mod3] = nameof(KeyCode.Mod3);

        var ordered = dict.OrderBy(x => (int)x.Key).ToArray();
        OrderedKeys = ordered;
        ToName = ordered.ToFrozenDictionary();
        ToKey = ordered.Select(kv => KeyValuePair.Create(kv.Value, kv.Key))
            .ToFrozenDictionary();
        
        // populate pretty names, using newlines for camel case
        MultilineNames = ordered.Select(kv =>
        {
            var name = kv.Value;
            var pretty = new System.Text.StringBuilder();
            for (var index = 0; index < name.Length; index++)
            {
                var c = name[index];
                if (index > 0 && char.IsUpper(c) && !char.IsUpper(name[index - 1]))
                {
                    pretty.Append('\n');
                }

                pretty.Append(c);
            }
            return KeyValuePair.Create(kv.Key, pretty.ToString());
        }).ToFrozenDictionary();
    }
}