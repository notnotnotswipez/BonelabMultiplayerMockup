using System.Collections.Generic;

namespace HBMP.Extensions
{
    public static class DictionaryExtensions
    {
        // This class is needed for the "SimplifiedTransforms" class by Entanglement devs.
        public static V TryIdx<K, V>(this Dictionary<K, V> dict, K idx)
        {
            if (dict.ContainsKey(idx)) return dict[idx];
            return default;
        }
    }
}