// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/EverythingEqual.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// An equality comparer that considers all values to be equal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EverythingEqual<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private static readonly EverythingEqual<T> s_singleton = new();

        private EverythingEqual() { }

        internal static EverythingEqual<T> Default
        {
            get
            {
                return s_singleton;
            }
        }

        public bool Equals(T? x, T? y)
        {
            return true;
        }

        public int GetHashCode(T obj)
        {
            return 1;
        }

        bool IEqualityComparer.Equals(object? x, object? y)
        {
            return true;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return 1;
        }
    }
}
