// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// Provides temporary storage for a collection of elements. This type is optimized for handling of small
    /// collections, particularly for cases where the collection will eventually be discarded or used to produce an
    /// <see cref="ImmutableArray{T}"/>.
    /// </summary>
    /// <remarks>
    /// This type stores small collections on the stack, with the ability to transition to dynamic storage if/when
    /// larger number of elements are added.
    /// </remarks>
    /// <typeparam name="T">The type of elements stored in the collection.</typeparam>
    [NonCopyable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TemporaryArray<T> : IDisposable
    {
        /// <summary>
        /// The number of elements the temporary can store inline. Storing more than this many elements requires the
        /// array transition to dynamic storage.
        /// </summary>
        private const int InlineCapacity = 4;

        /// <summary>
        /// The first inline element.
        /// </summary>
        /// <remarks>
        /// This field is only used when <see cref="_builder"/> is <see langword="null"/>. In other words, this type
        /// stores elements inline <em>or</em> stores them in <see cref="_builder"/>, but does not use both approaches
        /// at the same time.
        /// </remarks>
        private T _item0;

        /// <summary>
        /// The second inline element.
        /// </summary>
        /// <seealso cref="_item0"/>
        private T _item1;

        /// <summary>
        /// The third inline element.
        /// </summary>
        /// <seealso cref="_item0"/>
        private T _item2;

        /// <summary>
        /// The fourth inline element.
        /// </summary>
        /// <seealso cref="_item0"/>
        private T _item3;

        /// <summary>
        /// The number of inline elements held in the array. This value is only used when <see cref="_builder"/> is
        /// <see langword="null"/>.
        /// </summary>
        private int _count;

        /// <summary>
        /// A builder used for dynamic storage of collections that may exceed the limit for inline elements.
        /// </summary>
        /// <remarks>
        /// This field is initialized to non-<see langword="null"/> the first time the <see cref="TemporaryArray{T}"/>
        /// needs to store more than four elements. From that point, <see cref="_builder"/> is used instead of inline
        /// elements, even if items are removed to make the result smaller than four elements.
        /// </remarks>
        private ArrayBuilder<T>? _builder;

        private TemporaryArray(in TemporaryArray<T> array)
        {
            // Intentional copy used for creating an enumerator
#pragma warning disable RS0042 // Do not copy value
            this = array;
#pragma warning restore RS0042 // Do not copy value
        }

        public static TemporaryArray<T> GetInstance(int capacity)
        {
            // Capacity <= 4 is already supported by the Empty array value. so can just return that without allocating anything.
            if (capacity <= InlineCapacity)
                return Empty;

            return new TemporaryArray<T>()
            {
                _builder = ArrayBuilder<T>.GetInstance(capacity)
            };
        }

        public static TemporaryArray<T> Empty => default;

        public readonly int Count => _builder?.Count ?? _count;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                if (_builder is not null)
                    return _builder[index];

                if ((uint)index >= _count)
                    ThrowIndexOutOfRangeException();

                return index switch
                {
                    0 => _item0,
                    1 => _item1,
                    2 => _item2,
                    _ => _item3,
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_builder is not null)
                {
                    _builder[index] = value;
                    return;
                }

                if ((uint)index >= _count)
                    ThrowIndexOutOfRangeException();

                _ = index switch
                {
                    0 => _item0 = value,
                    1 => _item1 = value,
                    2 => _item2 = value,
                    _ => _item3 = value,
                };
            }
        }

        public void Dispose()
        {
            // Return _builder to the pool if necessary. There is no need to release inline storage since the majority
            // case for this type is stack-allocated storage and the GC is already able to reclaim objects from the
            // stack after the last use of a reference to them.
            Interlocked.Exchange(ref _builder, null)?.Free();
        }

        public void Add(T item)
        {
            if (_builder is not null)
            {
                _builder.Add(item);
            }
            else if (_count < InlineCapacity)
            {
                // Increase _count before assigning a value since the indexer has a bounds check.
                _count++;
                this[_count - 1] = item;
            }
            else
            {
                Debug.Assert(_count == InlineCapacity);
                MoveInlineToBuilder();
                _builder.Add(item);
            }
        }

        public void AddRange(ImmutableArray<T> items)
        {
            if (_builder is not null)
            {
                _builder.AddRange(items);
            }
            else if (_count + items.Length <= InlineCapacity)
            {
                foreach (var item in items)
                {
                    // Increase _count before assigning values since the indexer has a bounds check.
                    _count++;
                    this[_count - 1] = item;
                }
            }
            else
            {
                MoveInlineToBuilder();
                _builder.AddRange(items);
            }
        }

        public void AddRange(in TemporaryArray<T> items)
        {
            if (_count + items.Count <= InlineCapacity)
            {
                foreach (var item in items)
                {
                    // Increase _count before assigning values since the indexer has a bounds check.
                    _count++;
                    this[_count - 1] = item;
                }
            }
            else
            {
                MoveInlineToBuilder();
                foreach (var item in items)
                    _builder.Add(item);
            }
        }

        public void Clear()
        {
            if (_builder is not null)
            {
                // Keep using a builder even if we now fit in inline storage to avoid churn in the object pools.
                _builder.Clear();
            }
            else
            {
                this = Empty;
            }
        }

        public T RemoveLast()
        {
            var count = this.Count;

            var last = this[count - 1];
            this[count - 1] = default!;

            if (_builder != null)
            {
                _builder.Count--;
            }
            else
            {
                _count--;
            }

            return last;
        }

        public readonly bool Contains(T value)
        {
            if (_builder != null)
                return _builder.Contains(value);

            foreach (var v in this)
            {
                if (EqualityComparer<T>.Default.Equals(v, value))
                    return true;
            }

            return false;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(in this);
        }

        /// <summary>
        /// Create an <see cref="OneOrMany{T}"/> with the elements currently held in the temporary array, and clear the
        /// array.
        /// </summary>
        public OneOrMany<T> ToOneOrManyAndClear()
        {
            switch (this.Count)
            {
                case 0:
                    return OneOrMany<T>.Empty;
                case 1:
                    var result = OneOrMany.Create(this[0]);
                    this.Clear();
                    return result;
                default:
                    return new(this.ToImmutableAndClear());
            }
        }

        /// <summary>
        /// Create an <see cref="ImmutableArray{T}"/> with the elements currently held in the temporary array, and clear
        /// the array.
        /// </summary>
        public ImmutableArray<T> ToImmutableAndClear()
        {
            if (_builder is not null)
            {
                return _builder.ToImmutableAndClear();
            }
            else
            {
                var result = _count switch
                {
                    0 => ImmutableArray<T>.Empty,
                    1 => ImmutableArray.Create(_item0),
                    2 => ImmutableArray.Create(_item0, _item1),
                    3 => ImmutableArray.Create(_item0, _item1, _item2),
                    4 => ImmutableArray.Create(_item0, _item1, _item2, _item3),
                    _ => throw ExceptionUtilities.Unreachable(),
                };

                // Since _builder is null on this path, we can overwrite the whole structure to Empty to reset all
                // inline elements to their default value and the _count to 0.
                this = Empty;

                return result;
            }
        }

        /// <summary>
        /// Transitions the current <see cref="TemporaryArray{T}"/> from inline storage to dynamic storage storage. An
        /// <see cref="ArrayBuilder{T}"/> instance is taken from the shared pool, and all elements currently in inline
        /// storage are added to it. After this point, dynamic storage will be used instead of inline storage.
        /// </summary>
        [MemberNotNull(nameof(_builder))]
        private void MoveInlineToBuilder()
        {
            Debug.Assert(_builder is null);

            var builder = ArrayBuilder<T>.GetInstance();
            for (var i = 0; i < _count; i++)
            {
                builder.Add(this[i]);

#if NETCOREAPP
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
                {
                    this[i] = default!;
                }
            }

            _count = 0;
            _builder = builder;
        }

        public void ReverseContents()
        {
            if (_builder is not null)
            {
                _builder.ReverseContents();
                return;
            }

            switch (_count)
            {
                case <= 1:
                    // if we have one or zero items, we're already reversed.
                    return;
                case 2:
                    (_item0, _item1) = (_item1, _item0);
                    return;
                case 3:
                    // Just need to swap the first and last items.  The middle one stays where it is.
                    (_item0, _item2) = (_item2, _item0);
                    return;
                case 4:
                    (_item0, _item1, _item2, _item3) = (_item3, _item2, _item1, _item0);
                    return;
                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        public void Sort(Comparison<T> compare)
        {
            if (_builder is not null)
            {
                _builder.Sort(compare);
                return;
            }

            switch (_count)
            {
                case <= 1:
                    return;
                case 2:
                    if (compare(_item0, _item1) > 0)
                    {
                        (_item0, _item1) = (_item1, _item0);
                    }
                    return;
                case 3:
                    if (compare(_item0, _item1) > 0)
                        (_item0, _item1) = (_item1, _item0);

                    if (compare(_item1, _item2) > 0)
                    {
                        (_item1, _item2) = (_item2, _item1);

                        if (compare(_item0, _item1) > 0)
                            (_item0, _item1) = (_item1, _item0);
                    }
                    return;
                case 4:

                    if (compare(_item0, _item1) > 0)
                        (_item0, _item1) = (_item1, _item0);

                    if (compare(_item2, _item3) > 0)
                        (_item2, _item3) = (_item3, _item2);

                    if (compare(_item0, _item2) > 0)
                        (_item0, _item2) = (_item2, _item0);

                    if (compare(_item1, _item3) > 0)
                        (_item1, _item3) = (_item3, _item1);

                    if (compare(_item1, _item2) > 0)
                        (_item1, _item2) = (_item2, _item1);

                    return;
                default:
                    throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Throws <see cref="IndexOutOfRangeException"/>.
        /// </summary>
        /// <remarks>
        /// This helper improves the ability of the JIT to inline callers.
        /// </remarks>
        private static void ThrowIndexOutOfRangeException()
            => throw new IndexOutOfRangeException();

        [NonCopyable]
        public struct Enumerator
        {
            private readonly TemporaryArray<T> _array;

            private T _current;
            private int _nextIndex;

            public Enumerator(in TemporaryArray<T> array)
            {
                // Enumerate a copy of the original
                _array = new TemporaryArray<T>(in array);
                _current = default!;
                _nextIndex = 0;
            }

            public T Current => _current;

            public bool MoveNext()
            {
                if (_nextIndex >= _array.Count)
                {
                    return false;
                }
                else
                {
                    _current = _array[_nextIndex];
                    _nextIndex++;
                    return true;
                }
            }
        }

        internal static class TestAccessor
        {
            public static int InlineCapacity => TemporaryArray<T>.InlineCapacity;

            public static bool HasDynamicStorage(in TemporaryArray<T> array)
                => array._builder is not null;

            public static int InlineCount(in TemporaryArray<T> array)
                => array._count;
        }
    }
}
