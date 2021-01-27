using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.UBindr.Expressions
{
    public static class Extensions
    {
        public static TValue SafeGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                return default(TValue);
            }
            return value;
        }

        public static void TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                //throw new InvalidOperationException(string.Format("Dictionary allready contains {0}", key));
                return;
            }

            dictionary.Add(key, value);
        }

        public static void SafeAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value, bool replace = false)
        {
            if (dictionary.ContainsKey(key))
            {
                if (replace)
                {
                    dictionary[key] = value;
                }
            }
            else
            {
                dictionary.Add(key, value);
            }
        }

        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> constructor)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = constructor(key);
                dictionary.Add(key, value);
            }
            return value;
        }

        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> constructor)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = constructor();
                dictionary.Add(key, value);
            }
            return value;
        }

        public static string SJoin<T>(this IEnumerable<T> list, string separator = ", ")
        {
            return string.Join(separator, list.Select(x => x == null ? "" : x.ToString()).ToArray());
        }

        public static string SJoin<T>(this IEnumerable<T> list, Func<T, string> convert, string separator = ", ")
        {
            return string.Join(separator, list.Select(convert).ToArray());
        }

        public static IEnumerable<T> InReverse<T>(this IEnumerable<T> list)
        {
            return
                list.Reverse();
        }

        public static string Ellipsis(this string x, int maxLength)
        {
            if (x.Length < maxLength)
            {
                return x;
            }

            var n = x.Substring(0, maxLength) + "...";
            if (n.Length > x.Length)
            {
                return x;
            }
            else
            {
                return n;
            }
        }
    }
}