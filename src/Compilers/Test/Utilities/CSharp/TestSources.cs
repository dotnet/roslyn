// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal static class TestSources
    {
        internal static readonly string Span = @"
namespace System
{
    public readonly ref struct Span<T>
    {
        internal readonly T[] arr;
        internal readonly int start;

        public ref T this[int i] => ref arr[start + i];
        public override int GetHashCode() => 1;
        public int Length { get; }
        public bool IsEmpty => Length == 0;

        unsafe public Span(void* pointer, int length)
        {
            this.arr = Helpers.ToArray<T>(pointer, length);
            this.Length = length;
        }

        public Span(T[] arr)
        {
            this.arr = arr;
            this.Length = arr is null ? 0 : arr.Length;
        }

        public Span(T[] arr, int start, int length)
        {
            if (start + length > arr?.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.arr = arr;
            this.start = start;
            this.Length = length;
        }

        public void CopyTo(Span<T> other)
        {
            Array.Copy(arr, start, other.arr, other.start, Length);
        }

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly Span<T> _span;
            /// <summary>The next index to yield.</summary>
            private int _index;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name=""span"">The span to enumerate.</param>
            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref T Current
            {
                get => ref _span[_index];
            }
        }

        public static implicit operator Span<T>(T[] array) => new Span<T>(array);

        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new ReadOnlySpan<T>(span.arr);

        public Span<T> Slice(int offset, int length) => new Span<T>(this.arr, offset, length);
    }

    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;
        private readonly int start;

        public ref readonly T this[int i] => ref arr[start + i];
        public override int GetHashCode() => 2;
        public int Length { get; }
        public bool IsEmpty => Length == 0;

        unsafe public ReadOnlySpan(void* pointer, int length)
        {
            this.arr = Helpers.ToArray<T>(pointer, length);
            this.Length = length;
        }

        public ReadOnlySpan(T[] arr)
        {
            this.arr = arr;
            this.Length = arr is null ? 0 : arr.Length;
        }

        public ReadOnlySpan(T[] arr, int start, int length)
        {
            if (start + length > arr?.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.arr = arr;
            this.start = start;
            this.Length = length;
        }

        public void CopyTo(Span<T> other)
        {
            Array.Copy(arr, start, other.arr, other.start, Length);
        }

        /// <summary>Gets an enumerator for this span.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>The span being enumerated.</summary>
            private readonly ReadOnlySpan<T> _span;
            /// <summary>The next index to yield.</summary>
            private int _index;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name=""span"">The span to enumerate.</param>
            internal Enumerator(ReadOnlySpan<T> span)
            {
                _span = span;
                _index = -1;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public ref readonly T Current
            {
                get => ref _span[_index];
            }
        }

        public static implicit operator ReadOnlySpan<T>(T[] array) => array == null ? default : new ReadOnlySpan<T>(array);

        // NOTE: This is defined on String in the BCL (and the target type is non-generic ReadOnlySpan<char>).
        public static implicit operator ReadOnlySpan<T>(string stringValue) => string.IsNullOrEmpty(stringValue) ? default : new ReadOnlySpan<T>((T[])(object)stringValue.ToCharArray());

        public ReadOnlySpan<T> Slice(int offset, int length) => new ReadOnlySpan<T>(this.arr, offset, length);

#nullable enable
        public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> items) where TDerived : class?, T
        {
            return new ReadOnlySpan<T>(items.arr, items.start, items.Length);
        }
#nullable restore
    }

    public readonly ref struct SpanLike<T>
    {
        public readonly Span<T> field;
    }

    public enum Color: sbyte
    {
        Red,
        Green,
        Blue
    }

    public static unsafe class Helpers
    {
        public static T[] ToArray<T>(void* ptr, int count)
        {
            if (ptr == null)
            {
                return null;
            }

            if (typeof(T) == typeof(sbyte))
            {
                var arr = new sbyte[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((sbyte*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(byte))
            {
                var arr = new byte[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((byte*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(short))
            {
                var arr = new short[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((short*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(ushort))
            {
                var arr = new ushort[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((ushort*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(int))
            {
                var arr = new int[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((int*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(uint))
            {
                var arr = new uint[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((uint*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(long))
            {
                var arr = new long[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((long*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(ulong))
            {
                var arr = new ulong[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((ulong*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(char))
            {
                var arr = new char[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((char*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            if (typeof(T) == typeof(Color))
            {
                var arr = new Color[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((Color*)ptr)[i];
                }

                return (T[])(object)arr;
            }

            throw new Exception(""add a case for: "" + typeof(T));
        }
    }
}";

        internal static readonly string Index = @"

namespace System
{
    using System.Runtime.CompilerServices;
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (fromEnd)
                _value = ~value;
            else
                _value = value;
        }

        // The following private constructors mainly created for perf reason to avoid the checks
        private Index(int value)
        {
            _value = value;
        }

        /// <summary>Create an Index pointing at first element.</summary>
        public static Index Start => new Index(0);

        /// <summary>Create an Index pointing at beyond last element.</summary>
        public static Index End => new Index(~0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromStart(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new Index(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromEnd(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new Index(~value);
        }

        /// <summary>Returns the index value.</summary>
        public int Value
        {
            get
            {
                if (_value < 0)
                    return ~_value;
                else
                    return _value;
            }
        }

        public bool IsFromEnd => _value < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int length)
        {
            int offset;

            if (IsFromEnd)
                offset = length - (~_value);
            else
                offset = _value;

            return offset;
        }

        public override bool Equals(object value) => value is Index && _value == ((Index)value)._value;

        public bool Equals (Index other) => _value == other._value;

        public override int GetHashCode() => _value;

        public static implicit operator Index(int value) => FromStart(value);

        public override string ToString() => IsFromEnd ? ""^"" + Value.ToString() : Value.ToString();
    }
}";

        internal static readonly string Range = @"
namespace System
{
    using System.Runtime.CompilerServices;

    public readonly struct Range
    {
        public Index Start { get; }

        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public static Range StartAt(Index start) => new Range(start, Index.End);

        public static Range EndAt(Index end) => new Range(Index.Start, end);

        public static Range All => new Range(Index.Start, Index.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OffsetAndLength GetOffsetAndLength(int length)
        {
            int start;
            Index startIndex = Start;
            if (startIndex.IsFromEnd)
                start = length - startIndex.Value;
            else
                start = startIndex.Value;

            int end;
            Index endIndex = End;
            if (endIndex.IsFromEnd)
                end = length - endIndex.Value;
            else
                end = endIndex.Value;

            if ((uint)end > (uint)length || (uint)start > (uint)end)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new OffsetAndLength(start, end - start);
        }

        public readonly struct OffsetAndLength
        {
            public int Offset { get; }
            public int Length { get; }

            public OffsetAndLength(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }

            public void Deconstruct(out int offset, out int length)
            {
                offset = Offset;
                length = Length;
            }
        }

        public override string ToString() => $""{Start}..{End}"";
    }
}";

        public static readonly string GetSubArray = @"
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            Type elementType = array.GetType().GetElementType();
            var (offset, length) = range.GetOffsetAndLength(array.Length);

            T[] newArray = (T[])Array.CreateInstance(elementType, length);
            Array.Copy(array, offset, newArray, 0, length);
            return newArray;
        }
    }
}";

        public static readonly string ITuple = @"
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}";

        public static readonly string MemoryExtensions = @"
namespace System
{
    public static class MemoryExtensions
    {
        public static bool SequenceEqual<T> (this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            // unoptimized implementation for testing purposes
            if (span.Length != other.Length) return false;
            for(var i = 0; i < span.Length; i++)
            {
                if (!span[i].Equals(other[i]))
                    return false;
            }
            return true;
        }

        public static bool SequenceEqual<T> (this Span<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            // unoptimized implementation for testing purposes
            if (span.Length != other.Length) return false;
            for(var i = 0; i < span.Length; i++)
            {
                if (!span[i].Equals(other[i]))
                    return false;
            }
            return true;
        }

        public static ReadOnlySpan<char> AsSpan(this string text) => string.IsNullOrEmpty(text) ? default : new ReadOnlySpan<char>(text.ToCharArray());
    }
}";
    }
}
