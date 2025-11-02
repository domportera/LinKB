using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using InputHooks;

namespace LinKb.Core;

internal partial class KeyHandler
{
    private sealed class BinaryKeyStates : IReadOnlyDictionary<KeyCode, bool>
    {
        [ReadOnly(true)]
        private readonly KeyPressInfo[] _keyStatesReadOnly;
        private readonly KeyCode[] _allKeyCodesRaw;

        public BinaryKeyStates(KeyPressInfo[] keyStatesReadOnly, KeyCode[] allKeyCodesRaw) 
        {
            Debug.Assert(allKeyCodesRaw.Length == keyStatesReadOnly.Length);
            _keyStatesReadOnly = keyStatesReadOnly;
            _allKeyCodesRaw = allKeyCodesRaw;
        }

        public bool ContainsKey(KeyCode key) => (int)key < _keyStatesReadOnly.Length;
        
        public bool TryGetValue(KeyCode key, out bool value)
        {
            var asInt = (int)key;
            if (asInt >= _keyStatesReadOnly.Length)
            {
                value = false;
                return false;
            }
            
            value = _keyStatesReadOnly[asInt].Pressed;
            return true;
        }

        public bool this[KeyCode key] => _keyStatesReadOnly[(int)key].Pressed;

        public IEnumerable<KeyCode> Keys => _allKeyCodesRaw;

        public IEnumerable<bool> Values => _keyStatesReadOnly.Select(x => x.Pressed);

        public IEnumerator<KeyValuePair<KeyCode, bool>> GetEnumerator()
        {
            for(int i = 0; i < _keyStatesReadOnly.Length; i++)
            {
                yield return new KeyValuePair<KeyCode, bool>(_allKeyCodesRaw[i], _keyStatesReadOnly[i].Pressed);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _keyStatesReadOnly.Length;
    }
}