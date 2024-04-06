// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Represents a single item or many items (including none).
    /// </summary>
    /// <remarks>
    /// Used when a collection usually contains a single item but sometimes might contain multiple.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(OneOrMany<>.DebuggerProxy))]
    internal readonly struct OneOrMany<T>
    {
        public static readonly OneOrMany<T> Empty = new OneOrMany<T>(ImmutableArray<T>.Empty);

        private readonly T? _one;
        private readonly ImmutableArray<T> _many;

        public OneOrMany(T one)
        {
            _one = one;
            _many = default;
        }

        public OneOrMany(ImmutableArray<T> many)
        {
            if (many.IsDefault)
            {
                throw new ArgumentNullException(nameof(many));
            }

            if (many is [var item])
            {
                _one = item;
                _many = default;
            }
            else
            {
                _one = default;
                _many = many;
            }
        }

        /// <summary>
        /// True if the collection has a single item. This item is stored in <see cref="_one"/>.
        /// </summary>
        [MemberNotNullWhen(true, nameof(_one))]
        private bool HasOneItem
            => _many.IsDefault;

        public bool IsDefault
            => _one == null && _many.IsDefault;

        public T this[int index]
        {
            get
            {
                if (HasOneItem)
                {
                    if (index != 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return _one;
                }
                else
                {
                    return _many[index];
                }
            }
        }

        public int Count
            => HasOneItem ? 1 : _many.Length;

        public bool IsEmpty
            => Count == 0;

        public OneOrMany<T> Add(T item)
            => HasOneItem ? OneOrMany.Create(_one, item) :
               IsEmpty ? OneOrMany.Create(item) :
               OneOrMany.Create(_many.Add(item));

        public bool Contains(T item)
            => HasOneItem ? EqualityComparer<T>.Default.Equals(item, _one) : _many.Contains(item);

        public OneOrMany<T> RemoveAll(T item)
        {
            if (HasOneItem)
            {
                return EqualityComparer<T>.Default.Equals(item, _one) ? Empty : this;
            }

            return OneOrMany.Create(_many.WhereAsArray(static (value, item) => !EqualityComparer<T>.Default.Equals(value, item), item));
        }

        public OneOrMany<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            return HasOneItem ?
                OneOrMany.Create(selector(_one)) :
                OneOrMany.Create(_many.SelectAsArray(selector));
        }

        public OneOrMany<TResult> Select<TResult, TArg>(Func<T, TArg, TResult> selector, TArg arg)
        {
            return HasOneItem ?
                OneOrMany.Create(selector(_one, arg)) :
                OneOrMany.Create(_many.SelectAsArray(selector, arg));
        }

        public T First() => this[0];

        public T? FirstOrDefault()
            => HasOneItem ? _one : _many.FirstOrDefault();

        public T? FirstOrDefault(Func<T, bool> predicate)
        {
            if (HasOneItem)
            {
                return predicate(_one) ? _one : default;
            }

            return _many.FirstOrDefault(predicate);
        }

        public T? FirstOrDefault<TArg>(Func<T, TArg, bool> predicate, TArg arg)
        {
            if (HasOneItem)
            {
                return predicate(_one, arg) ? _one : default;
            }

            return _many.FirstOrDefault(predicate, arg);
        }

        public static OneOrMany<T> CastUp<TDerived>(OneOrMany<TDerived> from) where TDerived : class, T
        {
            return from.HasOneItem
                ? new OneOrMany<T>(from._one)
                : new OneOrMany<T>(ImmutableArray<T>.CastUp(from._many));
        }

        public bool All(Func<T, bool> predicate)
            => HasOneItem ? predicate(_one) : _many.All(predicate);

        public bool All<TArg>(Func<T, TArg, bool> predicate, TArg arg)
            => HasOneItem ? predicate(_one, arg) : _many.All(predicate, arg);

        public bool Any()
            => !IsEmpty;

        public bool Any(Func<T, bool> predicate)
            => HasOneItem ? predicate(_one) : _many.Any(predicate);

        public bool Any<TArg>(Func<T, TArg, bool> predicate, TArg arg)
            => HasOneItem ? predicate(_one, arg) : _many.Any(predicate, arg);

        public ImmutableArray<T> ToImmutable()
            => HasOneItem ? ImmutableArray.Create(_one) : _many;

        public T[] ToArray()
            => HasOneItem ? new[] { _one } : _many.ToArray();

        public bool SequenceEqual(OneOrMany<T> other, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (Count != other.Count)
            {
                return false;
            }

            Debug.Assert(HasOneItem == other.HasOneItem);

            return HasOneItem ? comparer.Equals(_one, other._one!) :
                   System.Linq.ImmutableArrayExtensions.SequenceEqual(_many, other._many, comparer);
        }

        public bool SequenceEqual(ImmutableArray<T> other, IEqualityComparer<T>? comparer = null)
            => SequenceEqual(OneOrMany.Create(other), comparer);

        public bool SequenceEqual(IEnumerable<T> other, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (!HasOneItem)
            {
                return _many.SequenceEqual(other, comparer);
            }

            var first = true;
            foreach (var otherItem in other)
            {
                if (!first || !comparer.Equals(_one, otherItem))
                {
                    return false;
                }

                first = false;
            }

            return true;
        }

        public Enumerator GetEnumerator()
            => new(this);

        internal struct Enumerator
        {
            private readonly OneOrMany<T> _collection;
            private int _index;

            internal Enumerator(OneOrMany<T> collection)
            {
                _collection = collection;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _collection.Count;
            }

            public T Current => _collection[_index];
        }

        private sealed class DebuggerProxy(OneOrMany<T> instance)
        {
            private readonly OneOrMany<T> _instance = instance;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items => _instance.ToArray();
        }

        private string GetDebuggerDisplay()
            => "Count = " + Count;
    }

    internal static class OneOrMany
    {
        public static OneOrMany<T> Create<T>(T one)
            => new OneOrMany<T>(one);

        public static OneOrMany<T> Create<T>(T one, T two)
            => new OneOrMany<T>(ImmutableArray.Create(one, two));

        public static OneOrMany<T> OneOrNone<T>(T? one)
            => one is null ? OneOrMany<T>.Empty : new OneOrMany<T>(one);

        public static OneOrMany<T> Create<T>(ImmutableArray<T> many)
            => new OneOrMany<T>(many);

        public static bool SequenceEqual<T>(this ImmutableArray<T> array, OneOrMany<T> other, IEqualityComparer<T>? comparer = null)
            => Create(array).SequenceEqual(other, comparer);

        public static bool SequenceEqual<T>(this IEnumerable<T> array, OneOrMany<T> other, IEqualityComparer<T>? comparer = null)
            => other.SequenceEqual(array, comparer);
    }
}
