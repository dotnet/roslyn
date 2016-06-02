// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System
{
    /// <summary>
    /// Helper so we can call some tuple methods recursively without knowing the underlying types.
    /// </summary>
    internal interface ITupleInternal : ITuple
    {
        string ToString(StringBuilder sb);
        int GetHashCode(IEqualityComparer comparer);
    }

    // This interface should also be implemented by System.Tuple and System.Collections.Generic.KeyValuePair
    // Allows value tuples to be used in pattern matching and decomposition
    public interface ITuple
    {
        int Size { get; }
        object this[int i] { get; }
    }

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

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) =>
            new ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>(item1, item2, item3, item4, item5, item6, item7, new ValueTuple<T8>(item8));

        // From System.Web.Util.HashCodeCombiner
        internal static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), h3);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), CombineHashCodes(h3, h4));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), h5);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7));
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7, h8));
        }
    }

    [Serializable]
    public struct ValueTuple<T1> : IEquatable<ValueTuple<T1>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            Item1 = item1;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default);
        }

        public Boolean Equals(ValueTuple<T1> other)
        {
            return Equals(Item1, other.Item1);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1>)) return false;

            var objTuple = (ValueTuple<T1>)other;

            return comparer.Equals(Item1, objTuple.Item1);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if(!(other is ValueTuple<T1>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1>)other;

            return comparer.Compare(Item1, objTuple.Item1);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(Item1);
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 1;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2>)) return false;

            var objTuple = (ValueTuple<T1, T2>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            return comparer.Compare(Item2, objTuple.Item2);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 2;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3> : IEquatable<ValueTuple<T1, T2, T3>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2, T3>)) return false;

            var objTuple = (ValueTuple<T1, T2, T3>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            return comparer.Compare(Item3, objTuple.Item3);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 3;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3, T4> : IEquatable<ValueTuple<T1, T2, T3, T4>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3, T4> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3) &&
                Equals(Item4, other.Item4);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (!(other is ValueTuple<T1, T2, T3, T4>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3) &&
                comparer.Equals(Item4, objTuple.Item4);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3, T4>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            c = comparer.Compare(Item3, objTuple.Item3);

            if (c != 0) return c;

            return comparer.Compare(Item4, objTuple.Item4);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3),
                                                comparer.GetHashCode(Item4));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(", ");
            sb.Append(Item4);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 4;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    case 4:
                        return Item4;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3, T4, T5> : IEquatable<ValueTuple<T1, T2, T3, T4, T5>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3, T4, T5> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3) &&
                Equals(Item4, other.Item4) && Equals(Item5, other.Item5);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2, T3, T4, T5>)) return false;

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3) &&
                comparer.Equals(Item4, objTuple.Item4) && comparer.Equals(Item5, objTuple.Item5);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3, T4, T5>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            c = comparer.Compare(Item3, objTuple.Item3);

            if (c != 0) return c;

            c = comparer.Compare(Item4, objTuple.Item4);

            if (c != 0) return c;

            return comparer.Compare(Item5, objTuple.Item5);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3),
                                                comparer.GetHashCode(Item4), comparer.GetHashCode(Item5));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(", ");
            sb.Append(Item4);
            sb.Append(", ");
            sb.Append(Item5);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 5;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    case 4:
                        return Item4;
                    case 5:
                        return Item5;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3, T4, T5, T6> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3) &&
                Equals(Item4, other.Item4) && Equals(Item5, other.Item5) && Equals(Item6, other.Item6);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2, T3, T4, T5, T6>)) return false;

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3) &&
                comparer.Equals(Item4, objTuple.Item4) && comparer.Equals(Item5, objTuple.Item5) && comparer.Equals(Item6, objTuple.Item6);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3, T4, T5, T6>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            c = comparer.Compare(Item3, objTuple.Item3);

            if (c != 0) return c;

            c = comparer.Compare(Item4, objTuple.Item4);

            if (c != 0) return c;

            c = comparer.Compare(Item5, objTuple.Item5);

            if (c != 0) return c;

            return comparer.Compare(Item6, objTuple.Item6);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3),
                                                comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(", ");
            sb.Append(Item4);
            sb.Append(", ");
            sb.Append(Item5);
            sb.Append(", ");
            sb.Append(Item6);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 6;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    case 4:
                        return Item4;
                    case 5:
                        return Item5;
                    case 6:
                        return Item6;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3) &&
                Equals(Item4, other.Item4) && Equals(Item5, other.Item5) && Equals(Item6, other.Item6) &&
                Equals(Item7, other.Item7);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2, T3, T4, T5, T6, T7>)) return false;

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3) &&
                comparer.Equals(Item4, objTuple.Item4) && comparer.Equals(Item5, objTuple.Item5) && comparer.Equals(Item6, objTuple.Item6) &&
                comparer.Equals(Item7, objTuple.Item7);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3, T4, T5, T6, T7>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            c = comparer.Compare(Item3, objTuple.Item3);

            if (c != 0) return c;

            c = comparer.Compare(Item4, objTuple.Item4);

            if (c != 0) return c;

            c = comparer.Compare(Item5, objTuple.Item5);

            if (c != 0) return c;

            c = comparer.Compare(Item6, objTuple.Item6);

            if (c != 0) return c;

            return comparer.Compare(Item7, objTuple.Item7);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3),
                                                comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6),
                                                comparer.GetHashCode(Item7));
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(", ");
            sb.Append(Item4);
            sb.Append(", ");
            sb.Append(Item5);
            sb.Append(", ");
            sb.Append(Item6);
            sb.Append(", ");
            sb.Append(Item7);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size
        {
            get
            {
                return 7;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    case 4:
                        return Item4;
                    case 5:
                        return Item5;
                    case 6:
                        return Item6;
                    case 7:
                        return Item7;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }

    [Serializable]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
        where TRest : ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            if (!(rest is ITupleInternal))
            {
                throw new ArgumentException();
            }

            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Rest = rest;
        }

        public override Boolean Equals(Object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<Object>.Default); ;
        }

        public Boolean Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2) && Equals(Item3, other.Item3) &&
                Equals(Item4, other.Item4) && Equals(Item5, other.Item5) && Equals(Item6, other.Item6) &&
                Equals(Item7, other.Item7) && Equals(Rest, other.Rest);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer)
        {
            if (other == null || !(other is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>)) return false;

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>)other;

            return comparer.Equals(Item1, objTuple.Item1) && comparer.Equals(Item2, objTuple.Item2) && comparer.Equals(Item3, objTuple.Item3) &&
                comparer.Equals(Item4, objTuple.Item4) && comparer.Equals(Item5, objTuple.Item5) && comparer.Equals(Item6, objTuple.Item6) &&
                comparer.Equals(Item7, objTuple.Item7) && comparer.Equals(Rest, objTuple.Rest);
        }

        Int32 IComparable.CompareTo(Object obj)
        {
            return ((IStructuralComparable)this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer)
        {
            if (other == null) return 1;

            if (!(other is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>))
            {
                throw new ArgumentException();
            }

            var objTuple = (ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>)other;

            int c = 0;

            c = comparer.Compare(Item1, objTuple.Item1);

            if (c != 0) return c;

            c = comparer.Compare(Item2, objTuple.Item2);

            if (c != 0) return c;

            c = comparer.Compare(Item3, objTuple.Item3);

            if (c != 0) return c;

            c = comparer.Compare(Item4, objTuple.Item4);

            if (c != 0) return c;

            c = comparer.Compare(Item5, objTuple.Item5);

            if (c != 0) return c;

            c = comparer.Compare(Item6, objTuple.Item6);

            if (c != 0) return c;

            c = comparer.Compare(Item7, objTuple.Item7);

            if (c != 0) return c;

            return comparer.Compare(Rest, objTuple.Rest);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<Object>.Default);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            // We want to have a limited hash in this case.  We'll use the last 8 elements of the tuple
            ITupleInternal t = (ITupleInternal)Rest;
            if (t.Size >= 8) { return t.GetHashCode(comparer); }

            // In this case, the rest memeber has less than 8 elements so we need to combine some our elements with the elements in rest
            int k = 8 - t.Size;
            switch (k)
            {
                case 1:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item7), t.GetHashCode(comparer));
                case 2:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), t.GetHashCode(comparer));
                case 3:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7),
                                                        t.GetHashCode(comparer));
                case 4:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6),
                                                        comparer.GetHashCode(Item7), t.GetHashCode(comparer));
                case 5:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5),
                                                        comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), t.GetHashCode(comparer));
                case 6:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4),
                                                        comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7),
                                                        t.GetHashCode(comparer));
                case 7:
                    return ValueTuple.CombineHashCodes(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3),
                                                        comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6),
                                                        comparer.GetHashCode(Item7), t.GetHashCode(comparer));
            }

            Contract.Assert(false, "Missed all cases for computing ValueTuple hash code");
            return -1;
        }

        Int32 ITupleInternal.GetHashCode(IEqualityComparer comparer)
        {
            return ((IStructuralEquatable)this).GetHashCode(comparer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITupleInternal)this).ToString(sb);
        }

        string ITupleInternal.ToString(StringBuilder sb)
        {
            sb.Append(Item1);
            sb.Append(", ");
            sb.Append(Item2);
            sb.Append(", ");
            sb.Append(Item3);
            sb.Append(", ");
            sb.Append(Item4);
            sb.Append(", ");
            sb.Append(Item5);
            sb.Append(", ");
            sb.Append(Item6);
            sb.Append(", ");
            sb.Append(Item7);
            sb.Append(", ");
            return ((ITupleInternal)Rest).ToString(sb);
        }

        int ITuple.Size
        {
            get
            {
                return 7 + ((ITupleInternal)Rest).Size;
            }
        }

        object ITuple.this[int i]
        {
            get
            {
                switch (i)
                {
                    case 1:
                        return Item1;
                    case 2:
                        return Item2;
                    case 3:
                        return Item3;
                    case 4:
                        return Item4;
                    case 5:
                        return Item5;
                    case 6:
                        return Item6;
                    case 7:
                        return Item7;
                    default:
                        return Rest[i - 7];
                }
            }
        }
    }
}