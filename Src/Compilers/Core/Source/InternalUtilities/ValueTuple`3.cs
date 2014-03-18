// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    // struct with three values
    internal struct ValueTuple<T1, T2, T3> : IEquatable<ValueTuple<T1, T2, T3>>
    {
        private static readonly EqualityComparer<T1> comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> comparer3 = EqualityComparer<T3>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }

        public bool Equals(ValueTuple<T1, T2, T3> other)
        {
            return comparer1.Equals(this.Item1, other.Item1) 
                && comparer2.Equals(this.Item2, other.Item2) 
                && comparer3.Equals(this.Item3, other.Item3);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3>)
            {
                var other = (ValueTuple<T1, T2, T3>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                Hash.Combine(
                    comparer1.GetHashCode(Item1),
                    comparer2.GetHashCode(Item2)),
                comparer3.GetHashCode(Item3));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3> left, ValueTuple<T1, T2, T3> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3> left, ValueTuple<T1, T2, T3> right)
        {
            return !left.Equals(right);
        }
    }
}