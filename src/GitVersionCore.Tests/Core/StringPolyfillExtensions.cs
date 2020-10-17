#if NETFRAMEWORK
using System;

namespace GitVersionCore.Tests
{
    public static class StringPolyfillExtensions
    {
        public static bool Contains(this string input, string value, StringComparison comparisonType)
        {
            return input.IndexOf(value, comparisonType) >= 0;
        }
    }
}
#endif
