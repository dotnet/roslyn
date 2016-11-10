using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal static class IReadOnlyListExtensions
    {
        public static T Last<T>(this IReadOnlyList<T> list)
        {
            return list[list.Count - 1];
        }
    }
}
