using System;
using System.Text;

namespace UMP.Services.Helpers
{
    internal static class Json
    {
        public static string GetKey(string key, string source)
        {
            var quotedKey = '"' + key + '"';
            var index = 0;

            while (true)
            {
                index = source.IndexOf(quotedKey, index);

                if (index == -1)
                    return string.Empty;

                index += quotedKey.Length;

                var start = index;
                start = source.SkipWhitespace(start);

                if (source[start++] != ':')
                    continue;

                start = source.SkipWhitespace(start);

                if (source[start++] != '"')
                    continue;

                var end = start;
                while (source[end] != '"')
                    end++;

                return source.Substring(start, end - start);
            }
        }
        public static bool TryGetKey(string key, string source, out string target)
        {
            return GetKey(key, source, out target);
        }
        public static string Extract(string source)
        {
            StringBuilder sb = new StringBuilder();
            int depth = 0;
            char lastChar = '\u0000';
            foreach (var ch in source)
            {
                sb.Append(ch);
                if (ch == '{' && lastChar != '\\')
                    depth++;
                else if (ch == '}' && lastChar != '\\')
                    depth--;
                if (depth == 0)
                    break;
                lastChar = ch;
            }
            return sb.ToString();
        }
        private static bool GetKey(string key, string source, out string target)
        {
            // Example scenario: "key" : "value"

            string quotedKey = '"' + key + '"';
            int index = 0;

            while (true)
            {
                index = source.IndexOf(quotedKey, index, StringComparison.Ordinal); // '"'
                if (index == -1)
                {
                    target = string.Empty;
                    return false;
                }
                index += quotedKey.Length; // ' '

                int start = index;
                start = source.SkipWhitespace(start); // ':'
                if (source[start++] != ':') // ' '
                    continue;
                start = source.SkipWhitespace(start); // '"'
                if (source[start++] != '"') // 'v'
                    continue;
                int end = start;
                while ((source[end - 1] == '\\' && source[end] == '"') || source[end] != '"') // "value\""
                {
                    end++;
                }
                target = source.Substring(start, end - start);
                return true;
            }
        }

    }
}
