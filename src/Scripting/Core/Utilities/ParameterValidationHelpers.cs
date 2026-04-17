// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class ParameterValidationHelpers
    {
        internal static ImmutableArray<T> CheckImmutableArray<T>(ImmutableArray<T> items, string parameterName)
        {
            if (items.IsDefault)
            {
                throw new ArgumentNullException(parameterName);
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    throw new ArgumentNullException($"{parameterName}[{i}]");
                }
            }

            return items;
        }

        internal static ImmutableArray<T> ToImmutableArrayChecked<T>(IEnumerable<T> items, string parameterName)
            where T : class
        {
            var builder = ArrayBuilder<T>.GetInstance();
            AddRangeChecked(builder, items, parameterName);
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<T> ConcatChecked<T>(ImmutableArray<T> existing, IEnumerable<T> items, string parameterName)
            where T : class
        {
            var builder = ArrayBuilder<T>.GetInstance();
            builder.AddRange(existing);
            AddRangeChecked(builder, items, parameterName);
            return builder.ToImmutableAndFree();
        }

        internal static void AddRangeChecked<T>(ArrayBuilder<T> builder, IEnumerable<T> items, string parameterName)
            where T : class
        {
            RequireNonNull(items, parameterName);

            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentNullException($"{parameterName}[{builder.Count}]");
                }

                builder.Add(item);
            }
        }

        internal static IEnumerable<S> SelectChecked<T, S>(IEnumerable<T> items, string parameterName, Func<T, S> selector)
            where T : class
            where S : class
        {
            RequireNonNull(items, parameterName);
            return items.Select(item => (item != null) ? selector(item) : null);
        }

        internal static void RequireNonNull<T>(IEnumerable<T> items, string parameterName)
        {
            if (items == null || items is ImmutableArray<T> && ((ImmutableArray<T>)items).IsDefault)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
