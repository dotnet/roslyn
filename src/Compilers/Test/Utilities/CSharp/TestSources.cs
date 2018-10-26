// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal static class TestSources
    {
        internal const string Span = @"
namespace System
{
    public readonly ref struct Span<T>
    {
        private readonly T[] arr;

        public ref T this[int i] => ref arr[i];
        public override int GetHashCode() => 1;
        public int Length { get; }

        unsafe public Span(void* pointer, int length)
        {
            this.arr = Helpers.ToArray<T>(pointer, length);
            this.Length = length;
        }

        public Span(T[] arr)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public void CopyTo(Span<T> other) { }

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
    }

    public readonly ref struct ReadOnlySpan<T>
    {
        private readonly T[] arr;

        public ref readonly T this[int i] => ref arr[i];
        public override int GetHashCode() => 2;
        public int Length { get; }

        unsafe public ReadOnlySpan(void* pointer, int length)
        {
            this.arr = Helpers.ToArray<T>(pointer, length);
            this.Length = length;
        }

        public ReadOnlySpan(T[] arr)
        {
            this.arr = arr;
            this.Length = arr.Length;
        }

        public void CopyTo(Span<T> other) { }

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

        public static implicit operator ReadOnlySpan<T>(string stringValue) => string.IsNullOrEmpty(stringValue) ? default : new ReadOnlySpan<T>((T[])(object)stringValue.ToCharArray());
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

            if (typeof(T) == typeof(int))
            {
                var arr = new int[count];
                for(int i = 0; i < count; i++)
                {
                    arr[i] = ((int*)ptr)[i];
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

        internal const string Index = @"
namespace System
{
    public readonly struct Index
    {
        private readonly int _value;

        public int Value => _value < 0 ? ~_value : _value;
        public bool FromEnd => _value < 0;

        public Index(int value, bool fromEnd)
        {
            if (value < 0)
            {
                throw new ArgumentException(""Index must not be negative."", nameof(value));
            }

            _value = fromEnd? ~value : value;
        }

        public static implicit operator Index(int value) => new Index(value, fromEnd: false);
    }
}";

        internal const string Range = @"
namespace System
{
    public readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }

        private Range(Index start, Index end)
        {
            this.Start = start;
            this.End = end;
        }

        public static Range Create(Index start, Index end) => new Range(start, end);
        public static Range FromStart(Index start) => new Range(start, new Index(0, fromEnd: true));
        public static Range ToEnd(Index end) => new Range(new Index(0, fromEnd: false), end);
        public static Range All() => new Range(new Index(0, fromEnd: false), new Index(0, fromEnd: true));
    }
}";
    }
}
