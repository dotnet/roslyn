// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable.Tests
{
    /// <summary>
    /// An equality comparer that considers all values to be equal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EverythingEqual<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private static EverythingEqual<T> s_singleton = new EverythingEqual<T>();

        private EverythingEqual() { }

        internal static EverythingEqual<T> Default
        {
            get
            {
                return s_singleton;
            }
        }

        public bool Equals(T x, T y)
        {
            return true;
        }

        public int GetHashCode(T obj)
        {
            return 1;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return true;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return 1;
        }
    }
}
