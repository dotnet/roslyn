// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.7/src/libraries/Common/tests/System/Collections/TestingTypes.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    #region Comparers and Equatables

    // Use parity only as a hashcode so as to have many collisions.
    [Serializable]
    public class BadIntEqualityComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y)
        {
            return x == y;
        }

        public int GetHashCode(int obj)
        {
            return obj % 2;
        }

        public override bool Equals(object? obj)
        {
            return obj is BadIntEqualityComparer; // Equal to all other instances of this type, not to anything else.
        }

        public override int GetHashCode()
        {
            return unchecked((int)0xC001CAFE); // Doesn't matter as long as its constant.
        }
    }

    [Serializable]
    public class EquatableBackwardsOrder : IEquatable<EquatableBackwardsOrder?>, IComparable<EquatableBackwardsOrder>, IComparable
    {
        private readonly int _value;

        public EquatableBackwardsOrder(int value)
        {
            _value = value;
        }

        public int CompareTo(EquatableBackwardsOrder? other) //backwards from the usual integer ordering
        {
            if (other is null)
                return -1;

            return other._value - _value;
        }

        public override int GetHashCode() => _value;

        public override bool Equals(object? obj)
        {
            return obj is EquatableBackwardsOrder other && Equals(other);
        }

        public bool Equals(EquatableBackwardsOrder? other)
        {
            return _value == other?._value;
        }

        int IComparable.CompareTo(object? obj)
        {
            if (obj != null && obj.GetType() == typeof(EquatableBackwardsOrder))
                return ((EquatableBackwardsOrder)obj)._value - _value;
            else return -1;
        }
    }

    [Serializable]
    public class Comparer_SameAsDefaultComparer : IEqualityComparer<int>, IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return x - y;
        }

        public bool Equals(int x, int y)
        {
            return x == y;
        }

        public int GetHashCode(int obj)
        {
            return obj.GetHashCode();
        }
    }

    [Serializable]
    public class Comparer_HashCodeAlwaysReturnsZero : IEqualityComparer<int>, IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return x - y;
        }

        public bool Equals(int x, int y)
        {
            return x == y;
        }

        public int GetHashCode(int obj)
        {
            return 0;
        }
    }

    [Serializable]
    public class Comparer_ModOfInt : IEqualityComparer<int>, IComparer<int>
    {
        private readonly int _mod;

        public Comparer_ModOfInt(int mod)
        {
            _mod = mod;
        }

        public Comparer_ModOfInt()
        {
            _mod = 500;
        }

        public int Compare(int x, int y)
        {
            return ((x % _mod) - (y % _mod));
        }

        public bool Equals(int x, int y)
        {
            return ((x % _mod) == (y % _mod));
        }

        public int GetHashCode(int x)
        {
            return (x % _mod);
        }
    }

    [Serializable]
    public class Comparer_AbsOfInt : IEqualityComparer<int>, IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return Math.Abs(x) - Math.Abs(y);
        }

        public bool Equals(int x, int y)
        {
            return Math.Abs(x) == Math.Abs(y);
        }

        public int GetHashCode(int x)
        {
            return Math.Abs(x);
        }
    }

    #endregion

    #region TestClasses

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

        public int CompareTo(object? obj)
        {
            if (obj?.GetType() == typeof(SimpleInt))
            {
                return ((SimpleInt)obj).Val - _val;
            }
            return -1;
        }

        public int CompareTo(object? other, IComparer comparer)
        {
            if (other?.GetType() == typeof(SimpleInt))
                return ((SimpleInt)other).Val - _val;
            return -1;
        }

        public bool Equals(object? other, IEqualityComparer comparer)
        {
            if (other?.GetType() == typeof(SimpleInt))
                return ((SimpleInt)other).Val == _val;
            return false;
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(_val);
        }
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

    public class GenericComparable : IComparable<GenericComparable>
    {
        private readonly int _value;

        public GenericComparable(int value)
        {
            _value = value;
        }

        public int CompareTo(GenericComparable? other) => _value.CompareTo(other?._value);
    }

    public class NonGenericComparable : IComparable
    {
        private readonly GenericComparable _inner;

        public NonGenericComparable(int value)
        {
            _inner = new GenericComparable(value);
        }

        public int CompareTo(object? other) =>
            _inner.CompareTo(((NonGenericComparable?)other)?._inner);
    }

    public class BadlyBehavingComparable : IComparable<BadlyBehavingComparable>, IComparable
    {
        public int CompareTo(BadlyBehavingComparable? other) => 1;

        public int CompareTo(object? other) => -1;
    }

    public class MutatingComparable : IComparable<MutatingComparable>, IComparable
    {
        private int _state;

        public MutatingComparable(int initialState)
        {
            _state = initialState;
        }

        public int State => _state;

        public int CompareTo(object? other) => _state++;

        public int CompareTo(MutatingComparable? other) => _state++;
    }

    public static class ValueComparable
    {
        // Convenience method so the compiler can work its type inference magic.
        public static ValueComparable<T> Create<T>(T value) where T : IComparable<T>
        {
            return new ValueComparable<T>(value);
        }
    }

    public readonly struct ValueComparable<T> : IComparable<ValueComparable<T>> where T : IComparable<T>
    {
        public ValueComparable(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public int CompareTo(ValueComparable<T> other) =>
            Value.CompareTo(other.Value);
    }

    public class Equatable : IEquatable<Equatable>
    {
        public Equatable(int value)
        {
            Value = value;
        }

        public int Value { get; }

        // Equals(object) is not implemented on purpose.
        // EqualityComparer is only supposed to call through to the strongly-typed Equals since we implement IEquatable.

        public bool Equals(Equatable? other)
        {
            return other != null && Value == other.Value;
        }

        public override int GetHashCode() => Value;
    }

    public struct NonEquatableValueType
    {
        public NonEquatableValueType(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }

    public class DelegateEquatable : IEquatable<DelegateEquatable>
    {
        public DelegateEquatable()
        {
            EqualsWorker = _ => false;
        }

        public Func<DelegateEquatable?, bool> EqualsWorker { get; set; }

        public bool Equals(DelegateEquatable? other) => EqualsWorker(other);
    }

    public struct ValueDelegateEquatable : IEquatable<ValueDelegateEquatable>
    {
        public Func<ValueDelegateEquatable, bool> EqualsWorker { get; set; }

        public bool Equals(ValueDelegateEquatable other) => EqualsWorker(other);
    }

    public sealed class TrackingEqualityComparer<T> : IEqualityComparer<T>
    {
        public int EqualsCalls { get; set; }
        public int GetHashCodeCalls { get; set; }

        public bool Equals(T? x, T? y)
        {
            EqualsCalls++;
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            GetHashCodeCalls++;
            return EqualityComparer<T>.Default.GetHashCode(obj!);
        }
    }

    #endregion
}
