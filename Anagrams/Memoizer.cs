using System;
using System.Collections.Generic;

namespace Anagrams
{
    public static class Memoizer
    {
        public static Func<T, TResult> Memoize<T, TResult>(Func<T, TResult> func)
        {
            var cache = new Dictionary<T, TResult>();

            return p1 =>
                {
                    var key = p1;
                    TResult val;
                    if (!cache.TryGetValue(key, out val))
                    {
                        val = func(key);
                        cache.Add(key, val);
                    }
                    return val;
                };
        }
    }
}
