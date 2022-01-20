using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendJoinedValues<T>(this StringBuilder builder, string separator, ImmutableArray<T> values, Action<T, StringBuilder> append)
        {
            var first = true;
            foreach (var value in values)
            {
                if (!first)
                {
                    builder.Append(separator);
                }

                first = false;
                append(value, builder);
            }

            return builder;
        }
    }
}
