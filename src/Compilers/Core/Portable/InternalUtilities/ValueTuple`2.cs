// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    // struct with two values
    internal struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public bool Equals(ValueTuple<T1, T2> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2>)
            {
                var other = (ValueTuple<T1, T2>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2));
        }

        public static bool operator ==(ValueTuple<T1, T2> left, ValueTuple<T1, T2> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2> left, ValueTuple<T1, T2> right)
        {
            return !left.Equals(right);
        }
    }
}
