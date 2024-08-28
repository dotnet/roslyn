// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    /// <summary>
    /// Provides static methods to invoke <see cref="IDictionary"/> members on value types that explicitly implement the
    /// member.
    /// </summary>
    /// <remarks>
    /// Normally, invocation of explicit interface members requires boxing or copying the value type, which is
    /// especially problematic for operations that mutate the value. Invocation through these helpers behaves like a
    /// normal call to an implicitly implemented member.
    /// </remarks>
    internal static class IDictionaryCalls
    {
        public static bool IsFixedSize<TDictionary>(ref TDictionary dictionary)
            where TDictionary : IDictionary
            => dictionary.IsFixedSize;

        public static bool IsReadOnly<TDictionary>(ref TDictionary dictionary)
            where TDictionary : IDictionary
            => dictionary.IsReadOnly;

        public static object? GetItem<TDictionary>(ref TDictionary dictionary, object key)
            where TDictionary : IDictionary
            => dictionary[key];

        public static void SetItem<TDictionary>(ref TDictionary dictionary, object key, object? value)
            where TDictionary : IDictionary
            => dictionary[key] = value;

        public static void Add<TDictionary>(ref TDictionary dictionary, object key, object? value)
            where TDictionary : IDictionary
            => dictionary.Add(key, value);

        public static bool Contains<TDictionary>(ref TDictionary dictionary, object key)
            where TDictionary : IDictionary
            => dictionary.Contains(key);

        public static void CopyTo<TDictionary>(ref TDictionary dictionary, Array array, int index)
            where TDictionary : IDictionary
            => dictionary.CopyTo(array, index);

        public static IDictionaryEnumerator GetEnumerator<TDictionary>(ref TDictionary dictionary)
            where TDictionary : IDictionary
            => dictionary.GetEnumerator();

        public static void Remove<TDictionary>(ref TDictionary dictionary, object key)
            where TDictionary : IDictionary
            => dictionary.Remove(key);
    }
}
