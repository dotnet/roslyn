using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class MefExtensions
    {
        public static IEnumerable<Lazy<T>> RealizeImports<T>(this IEnumerable<Lazy<T>> lazyImports)
        {
            foreach (var lazyImport in lazyImports)
            {
                var unused = lazyImport.Value;
            }

            return lazyImports;
        }

        public static IEnumerable<Lazy<T, TMetadata>> RealizeImports<T, TMetadata>(this IEnumerable<Lazy<T, TMetadata>> lazyImports)
        {
            foreach (var lazyImport in lazyImports)
            {
                var unused = lazyImport.Value;
            }

            return lazyImports;
        }
    }
}