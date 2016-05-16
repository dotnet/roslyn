using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
