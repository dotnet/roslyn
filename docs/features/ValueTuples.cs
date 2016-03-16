// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

internal static class Hash
{
   /// <summary>
   /// This is how VB Anonymous Types combine hash values for fields.
   /// </summary>
   internal static int Combine(int newKey, int currentKey)
   {
      return unchecked((currentKey * (int)0xA5555529) + newKey);
   }
}

namespace System
{
    public static class ValueTuple
    {
        public static ValueTuple<T1> Create<T1>(T1 item1) =>
            new ValueTuple<T1>(item1);

        public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) =>
            new ValueTuple<T1, T2>(item1, item2);

        public static ValueTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) =>
            new ValueTuple<T1, T2, T3>(item1, item2, item3);

        public static ValueTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) =>
            new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);

        public static ValueTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) =>
            new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);

        public static ValueTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) =>
            new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) =>
            new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> Create<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
            where TRest : IEquatable<TRest>
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }
    }

    // struct with one value
    public struct ValueTuple<T1> : IEquatable<ValueTuple<T1>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;

        public readonly T1 Item1;

        public ValueTuple(T1 item1)
        {
            this.Item1 = item1;
        }

        public bool Equals(ValueTuple<T1> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1>)
            {
                var other = (ValueTuple<T1>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return s_comparer1.GetHashCode(Item1);
        }

        public static bool operator ==(ValueTuple<T1> left, ValueTuple<T1> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1> left, ValueTuple<T1> right)
        {
            return !left.Equals(right);
        }
    }

    // struct with two values
    public struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>
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

    // struct with three values
    public struct ValueTuple<T1, T2, T3> : IEquatable<ValueTuple<T1, T2, T3>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;

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
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3);
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
                        Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)), 
                        s_comparer3.GetHashCode(Item3));
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

    // struct with four values
    public struct ValueTuple<T1, T2, T3, T4> : IEquatable<ValueTuple<T1, T2, T3, T4>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;
        private static readonly EqualityComparer<T4> s_comparer4 = EqualityComparer<T4>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
        public readonly T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }

        public bool Equals(ValueTuple<T1, T2, T3, T4> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3)
                && s_comparer4.Equals(this.Item4, other.Item4);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3, T4>)
            {
                var other = (ValueTuple<T1, T2, T3, T4>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                        Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)),
                        Hash.Combine(s_comparer3.GetHashCode(Item3), s_comparer4.GetHashCode(Item4)));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4> left, ValueTuple<T1, T2, T3, T4> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3, T4> left, ValueTuple<T1, T2, T3, T4> right)
        {
            return !left.Equals(right);
        }
    }

    // struct with five values
    public struct ValueTuple<T1, T2, T3, T4, T5> : IEquatable<ValueTuple<T1, T2, T3, T4, T5>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;
        private static readonly EqualityComparer<T4> s_comparer4 = EqualityComparer<T4>.Default;
        private static readonly EqualityComparer<T5> s_comparer5 = EqualityComparer<T5>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
        public readonly T4 Item4;
        public readonly T5 Item5;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
        }

        public bool Equals(ValueTuple<T1, T2, T3, T4, T5> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3)
                && s_comparer4.Equals(this.Item4, other.Item4)
                && s_comparer5.Equals(this.Item5, other.Item5);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3, T4, T5>)
            {
                var other = (ValueTuple<T1, T2, T3, T4, T5>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                        Hash.Combine(
                            Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)),
                            Hash.Combine(s_comparer3.GetHashCode(Item3), s_comparer4.GetHashCode(Item4))),
                        s_comparer5.GetHashCode(Item5));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5> left, ValueTuple<T1, T2, T3, T4, T5> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5> left, ValueTuple<T1, T2, T3, T4, T5> right)
        {
            return !left.Equals(right);
        }
    }

    // struct with six values
    public struct ValueTuple<T1, T2, T3, T4, T5, T6> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;
        private static readonly EqualityComparer<T4> s_comparer4 = EqualityComparer<T4>.Default;
        private static readonly EqualityComparer<T5> s_comparer5 = EqualityComparer<T5>.Default;
        private static readonly EqualityComparer<T6> s_comparer6 = EqualityComparer<T6>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
        public readonly T4 Item4;
        public readonly T5 Item5;
        public readonly T6 Item6;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
        }

        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3)
                && s_comparer4.Equals(this.Item4, other.Item4)
                && s_comparer5.Equals(this.Item5, other.Item5)
                && s_comparer6.Equals(this.Item6, other.Item6);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3, T4, T5, T6>)
            {
                var other = (ValueTuple<T1, T2, T3, T4, T5, T6>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                        Hash.Combine(
                            Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)),
                            Hash.Combine(s_comparer3.GetHashCode(Item3), s_comparer4.GetHashCode(Item4))),
                        Hash.Combine(s_comparer5.GetHashCode(Item5), s_comparer6.GetHashCode(Item6)));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5, T6> left, ValueTuple<T1, T2, T3, T4, T5, T6> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5, T6> left, ValueTuple<T1, T2, T3, T4, T5, T6> right)
        {
            return !left.Equals(right);
        }
    }

    // struct with seven values
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;
        private static readonly EqualityComparer<T4> s_comparer4 = EqualityComparer<T4>.Default;
        private static readonly EqualityComparer<T5> s_comparer5 = EqualityComparer<T5>.Default;
        private static readonly EqualityComparer<T6> s_comparer6 = EqualityComparer<T6>.Default;
        private static readonly EqualityComparer<T7> s_comparer7 = EqualityComparer<T7>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
        public readonly T4 Item4;
        public readonly T5 Item5;
        public readonly T6 Item6;
        public readonly T7 Item7;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
        }

        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3)
                && s_comparer4.Equals(this.Item4, other.Item4)
                && s_comparer5.Equals(this.Item5, other.Item5)
                && s_comparer6.Equals(this.Item6, other.Item6)
                && s_comparer7.Equals(this.Item7, other.Item7);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3, T4, T5, T6, T7>)
            {
                var other = (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                        Hash.Combine(
                            Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)),
                            Hash.Combine(s_comparer3.GetHashCode(Item3), s_comparer4.GetHashCode(Item4))),
                        Hash.Combine(
                            Hash.Combine(s_comparer5.GetHashCode(Item5), s_comparer6.GetHashCode(Item6)),
                            s_comparer7.GetHashCode(Item7)));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5, T6, T7> left, ValueTuple<T1, T2, T3, T4, T5, T6, T7> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5, T6, T7> left, ValueTuple<T1, T2, T3, T4, T5, T6, T7> right)
        {
            return !left.Equals(right);
        }
    }

    // struct with seven values and an extension
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
      where TRest : IEquatable<TRest>
    {
        private static readonly EqualityComparer<T1> s_comparer1 = EqualityComparer<T1>.Default;
        private static readonly EqualityComparer<T2> s_comparer2 = EqualityComparer<T2>.Default;
        private static readonly EqualityComparer<T3> s_comparer3 = EqualityComparer<T3>.Default;
        private static readonly EqualityComparer<T4> s_comparer4 = EqualityComparer<T4>.Default;
        private static readonly EqualityComparer<T5> s_comparer5 = EqualityComparer<T5>.Default;
        private static readonly EqualityComparer<T6> s_comparer6 = EqualityComparer<T6>.Default;
        private static readonly EqualityComparer<T7> s_comparer7 = EqualityComparer<T7>.Default;

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
        public readonly T4 Item4;
        public readonly T5 Item5;
        public readonly T6 Item6;
        public readonly T7 Item7;
        public readonly TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
            this.Rest = rest;
        }

        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other)
        {
            return s_comparer1.Equals(this.Item1, other.Item1)
                && s_comparer2.Equals(this.Item2, other.Item2)
                && s_comparer3.Equals(this.Item3, other.Item3)
                && s_comparer4.Equals(this.Item4, other.Item4)
                && s_comparer5.Equals(this.Item5, other.Item5)
                && s_comparer6.Equals(this.Item6, other.Item6)
                && s_comparer7.Equals(this.Item7, other.Item7)
                && this.Rest.Equals(other.Rest);
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>)
            {
                var other = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>)obj;
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                        Hash.Combine(
                            Hash.Combine(s_comparer1.GetHashCode(Item1), s_comparer2.GetHashCode(Item2)),
                            Hash.Combine(s_comparer3.GetHashCode(Item3), s_comparer4.GetHashCode(Item4))),
                        Hash.Combine(
                            Hash.Combine(s_comparer5.GetHashCode(Item5), s_comparer6.GetHashCode(Item6)),
                            Hash.Combine(s_comparer7.GetHashCode(Item7), this.Rest.GetHashCode())));
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> left, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> left, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> right)
        {
            return !left.Equals(right);
        }
    }
}

