// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    /// <summary>
    /// Provides static methods to invoke <see cref="IList"/> members on value types that explicitly implement the
    /// member.
    /// </summary>
    /// <remarks>
    /// Normally, invocation of explicit interface members requires boxing or copying the value type, which is
    /// especially problematic for operations that mutate the value. Invocation through these helpers behaves like a
    /// normal call to an implicitly implemented member.
    /// </remarks>
    internal static class IListCalls
    {
        public static object? GetItem<TList>(ref TList list, int index)
            where TList : IList
            => list[index];

        public static void SetItem<TList>(ref TList list, int index, object? value)
            where TList : IList
            => list[index] = value;

        public static bool IsFixedSize<TList>(ref TList list)
            where TList : IList
            => list.IsFixedSize;

        public static bool IsReadOnly<TList>(ref TList list)
            where TList : IList
            => list.IsReadOnly;

        public static int Add<TList>(ref TList list, object? value)
            where TList : IList
        {
            return list.Add(value);
        }

        public static bool Contains<TList>(ref TList list, object? value)
            where TList : IList
        {
            return list.Contains(value);
        }

        public static int IndexOf<TList>(ref TList list, object? value)
            where TList : IList
        {
            return list.IndexOf(value);
        }

        public static void Insert<TList>(ref TList list, int index, object? value)
            where TList : IList
        {
            list.Insert(index, value);
        }

        public static void Remove<TList>(ref TList list, object? value)
            where TList : IList
        {
            list.Remove(value);
        }
    }
}
