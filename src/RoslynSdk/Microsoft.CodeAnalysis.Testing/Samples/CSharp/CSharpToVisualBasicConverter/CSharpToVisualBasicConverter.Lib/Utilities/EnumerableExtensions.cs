// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace CSharpToVisualBasicConverter.Utilities
{
    internal static class EnumerableExtensions
    {
        private static IEnumerable<T> ConcatWorker<T>(this IEnumerable<T> source, T value)
        {
            foreach (T v in source)
            {
                yield return v;
            }

            yield return value;
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ConcatWorker(value);
        }
    }
}
