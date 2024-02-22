// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace System.Linq
{
    internal static class RoslynEnumerable
    {
        public static SegmentedList<TSource> ToSegmentedList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);

            return new SegmentedList<TSource>(source);
        }

        public static bool TryGetCount<T>(this IEnumerable<T> source, out int count)
        {
            if (source is IReadOnlyCollection<T> readOnlyCollection)
            {
                count = readOnlyCollection.Count;
                return true;
            }

            if (source is ICollection<T> genericCollection)
            {
                count = genericCollection.Count;
                return true;
            }

            if (source is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            if (source is string str)
            {
                count = str.Length;
                return true;
            }

            count = 0;
            return false;
        }
    }
}
