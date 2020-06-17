using System.Collections.Generic;

namespace ShowMeTheXAML
{
    public static class XamlResolver
    {
        private static Dictionary<string, string> XamlByKey { get; } = new Dictionary<string, string>();

        public static IReadOnlyDictionary<string, string> DebugView => XamlByKey;

        public static void Set(string key, string xaml)
            => XamlByKey[key] = xaml;

        public static string Resolve(string key)
        {
            if (key != null && XamlByKey.TryGetValue(key, out string xaml))
            {
                return xaml;
            }
            return "";
        }
    }
}