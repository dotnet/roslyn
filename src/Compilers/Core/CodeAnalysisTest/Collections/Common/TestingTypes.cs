// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{

    public sealed class TrackingEqualityComparer<T> : IEqualityComparer<T>
    {
        public int EqualsCalls;
        public int GetHashCodeCalls;

#nullable disable

        public bool Equals(T x, T y)
        {
            EqualsCalls++;
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            GetHashCodeCalls++;
            return EqualityComparer<T>.Default.GetHashCode(obj);
        }

#nullable enable
    }

    [Serializable]
    public class WrapStructural_Int : IEqualityComparer<int>, IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return StructuralComparisons.StructuralComparer.Compare(x, y);
        }

        public bool Equals(int x, int y)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
        }

        public int GetHashCode(int obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }

    [Serializable]
    public class WrapStructural_SimpleInt : IEqualityComparer<SimpleInt>, IComparer<SimpleInt>
    {
        public int Compare(SimpleInt x, SimpleInt y)
        {
            return StructuralComparisons.StructuralComparer.Compare(x, y);
        }

        public bool Equals(SimpleInt x, SimpleInt y)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
        }

        public int GetHashCode(SimpleInt obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }

    [Serializable]
    public struct SimpleInt : IStructuralComparable, IStructuralEquatable, IComparable, IComparable<SimpleInt>
    {
        private int _val;
        public SimpleInt(int t)
        {
            _val = t;
        }
        public int Val
        {
            get { return _val; }
            set { _val = value; }
        }

        public int CompareTo(SimpleInt other)
        {
            return other.Val - _val;
        }

#nullable disable

        public int CompareTo(object obj)
        {
            if (obj.GetType() == typeof(SimpleInt))
            {
                return ((SimpleInt)obj).Val - _val;
            }
            return -1;
        }

        public int CompareTo(object other, IComparer comparer)
        {
            if (other.GetType() == typeof(SimpleInt))
                return ((SimpleInt)other).Val - _val;
            return -1;
        }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            if (other.GetType() == typeof(SimpleInt))
                return ((SimpleInt)other).Val == _val;
            return false;
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(_val);
        }
    }
}
