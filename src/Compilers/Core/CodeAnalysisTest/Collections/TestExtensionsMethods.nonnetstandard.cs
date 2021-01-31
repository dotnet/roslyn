// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/TestExtensionsMethods.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    internal static partial class TestExtensionsMethods
    {
        internal static IDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return (IDictionary<TKey, TValue>)dictionary;
        }

        internal static IDictionary<TKey, TValue> ToBuilder<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            return dictionary switch
            {
                ImmutableDictionary<TKey, TValue> d => d.ToBuilder(),
                ImmutableSortedDictionary<TKey, TValue> d => d.ToBuilder(),
                ImmutableSegmentedDictionary<TKey, TValue> d => d.ToBuilder(),
                null => throw new ArgumentNullException(nameof(dictionary)),
                _ => throw ExceptionUtilities.UnexpectedValue(dictionary),
            };
        }

        internal static IEqualityComparer<TKey> GetKeyComparer<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            return dictionary switch
            {
                ImmutableDictionary<TKey, TValue> d => d.KeyComparer,
                ImmutableSegmentedDictionary<TKey, TValue> d => d.KeyComparer,
                null => throw new ArgumentNullException(nameof(dictionary)),
                _ => throw ExceptionUtilities.UnexpectedValue(dictionary),
            };
        }
    }
}
