// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Threading;

#if DEBUG
using System.Diagnostics;
#endif

namespace Roslyn.Utilities
{
    internal static partial class EnumerableExtensions
    {
        public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // perf optimization. try to not use enumerator if possible
            if (source is IList<T> list)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                {
                    action(list[i]);
                }
            }
            else
            {
                foreach (var value in source)
                {
                    action(value);
                }
            }

            return source;
        }

        public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? items)
        {
            if (items == null)
            {
                return ImmutableArray.Create<T>();
            }

            if (items is ImmutableArray<T> array)
            {
                return array.NullToEmpty();
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        public static IReadOnlyList<T> ToBoxedImmutableArray<T>(this IEnumerable<T>? items)
        {
            if (items is null)
            {
                return SpecializedCollections.EmptyBoxedImmutableArray<T>();
            }

            if (items is ImmutableArray<T> array)
            {
                return array.IsDefaultOrEmpty ? SpecializedCollections.EmptyBoxedImmutableArray<T>() : (IReadOnlyList<T>)items;
            }

            if (items is ICollection<T> collection && collection.Count == 0)
            {
                return SpecializedCollections.EmptyBoxedImmutableArray<T>();
            }

            return ImmutableArray.CreateRange(items);
        }

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyCollection<T>(source.ToList());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ConcatWorker(value);
        }

        private static IEnumerable<T> ConcatWorker<T>(this IEnumerable<T> source, T value)
        {
            foreach (var v in source)
            {
                yield return v;
            }

            yield return value;
        }

        public static bool SetEquals<T>(this IEnumerable<T> source1, IEnumerable<T> source2, IEqualityComparer<T>? comparer)
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            if (source2 == null)
            {
                throw new ArgumentNullException(nameof(source2));
            }

            return source1.ToSet(comparer).SetEquals(source2);
        }

        public static bool SetEquals<T>(this IEnumerable<T> source1, IEnumerable<T> source2)
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            if (source2 == null)
            {
                throw new ArgumentNullException(nameof(source2));
            }

            return source1.ToSet().SetEquals(source2);
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new HashSet<T>(source, comparer);
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source as ISet<T> ?? new HashSet<T>(source);
        }

        public static IReadOnlyCollection<T> ToCollection<T>(this IEnumerable<T> sequence)
            => (sequence is IReadOnlyCollection<T> collection) ? collection : sequence.ToList();

        public static T? FirstOrDefault<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            foreach (var item in source)
            {
                if (predicate(item, arg))
                    return item;
            }

            return default;
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> source)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().FirstOrDefault();
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return source.Cast<T?>().FirstOrDefault(static (v, predicate) => predicate(v!.Value), predicate);
        }

        public static T? FirstOrNull<T, TArg>(this IEnumerable<T> source, Func<T, TArg, bool> predicate, TArg arg)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return source.Cast<T?>().FirstOrDefault(static (v, arg) => arg.predicate(v!.Value, arg.arg), (predicate, arg));
        }

        public static T? LastOrNull<T>(this IEnumerable<T> source)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().LastOrDefault();
        }

        public static T? SingleOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            where T : struct
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Cast<T?>().SingleOrDefault(v => predicate(v!.Value));
        }

        public static bool IsSingle<T>(this IEnumerable<T> list)
        {
            using var enumerator = list.GetEnumerator();
            return enumerator.MoveNext() && !enumerator.MoveNext();
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            if (source is IReadOnlyCollection<T> readOnlyCollection)
            {
                return readOnlyCollection.Count == 0;
            }

            if (source is ICollection<T> genericCollection)
            {
                return genericCollection.Count == 0;
            }

            if (source is ICollection collection)
            {
                return collection.Count == 0;
            }

            if (source is string str)
            {
                return str.Length == 0;
            }

            foreach (var _ in source)
            {
                return false;
            }

            return true;
        }

        public static bool IsEmpty<T>(this IReadOnlyCollection<T> source)
        {
            return source.Count == 0;
        }

        public static bool IsEmpty<T>(this ICollection<T> source)
        {
            return source.Count == 0;
        }

        public static bool IsEmpty(this string source)
        {
            return source.Length == 0;
        }

        /// <remarks>
        /// This method is necessary to avoid an ambiguity between <see cref="IsEmpty{T}(IReadOnlyCollection{T})"/> and <see cref="IsEmpty{T}(ICollection{T})"/>.
        /// </remarks>
        public static bool IsEmpty<T>(this T[] source)
        {
            return source.Length == 0;
        }

        /// <remarks>
        /// This method is necessary to avoid an ambiguity between <see cref="IsEmpty{T}(IReadOnlyCollection{T})"/> and <see cref="IsEmpty{T}(ICollection{T})"/>.
        /// </remarks>
        public static bool IsEmpty<T>(this List<T> source)
        {
            return source.Count == 0;
        }

        private static readonly Func<object, bool> s_notNullTest = x => x != null;

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            if (source == null)
            {
                return SpecializedCollections.EmptyEnumerable<T>();
            }

            return source.Where((Func<T?, bool>)s_notNullTest)!;
        }

        public static T[] AsArray<T>(this IEnumerable<T> source)
            => source as T[] ?? source.ToArray();

        public static ImmutableArray<TResult> SelectAsArray<TSource, TResult>(this IEnumerable<TSource>? source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                return ImmutableArray<TResult>.Empty;
            }

            var builder = ArrayBuilder<TResult>.GetInstance();
            builder.AddRange(source.Select(selector));

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectAsArray<TSource, TResult>(this IEnumerable<TSource>? source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            var builder = ArrayBuilder<TResult>.GetInstance();

            int index = 0;
            foreach (var element in source)
            {
                builder.Add(selector(element, index));
                index++;
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectAsArray<TSource, TResult>(this IReadOnlyCollection<TSource>? source, Func<TSource, TResult> selector)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            var builder = ArrayBuilder<TResult>.GetInstance(source.Count);
            foreach (var item in source)
                builder.Add(selector(item));

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectManyAsArray<TSource, TResult>(this IEnumerable<TSource>? source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in source)
                builder.AddRange(selector(item));

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TArg, TResult>(this IEnumerable<TItem>? source, Func<TItem, TArg, IEnumerable<TResult>> selector, TArg arg)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in source)
                builder.AddRange(selector(item, arg));

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this IReadOnlyCollection<TItem>? source, Func<TItem, IEnumerable<TResult>> selector)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            // Basic heuristic. Assume each element in the source adds one item to the result.
            var builder = ArrayBuilder<TResult>.GetInstance(source.Count);
            foreach (var item in source)
                builder.AddRange(selector(item));

            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TArg, TResult>(this IReadOnlyCollection<TItem>? source, Func<TItem, TArg, IEnumerable<TResult>> selector, TArg arg)
        {
            if (source == null)
                return ImmutableArray<TResult>.Empty;

            // Basic heuristic. Assume each element in the source adds one item to the result.
            var builder = ArrayBuilder<TResult>.GetInstance(source.Count);
            foreach (var item in source)
                builder.AddRange(selector(item, arg));

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTask, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TResult>(this IEnumerable<TItem> source, Func<TItem, ValueTask<TResult>> selector)
        {
            var builder = ArrayBuilder<TResult>.GetInstance();

            foreach (var item in source)
            {
                builder.Add(await selector(item).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTask, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TResult>(this IEnumerable<TItem> source, Func<TItem, CancellationToken, ValueTask<TResult>> selector, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<TResult>.GetInstance();

            foreach (var item in source)
            {
                builder.Add(await selector(item, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTask, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TArg, TResult>(this IEnumerable<TItem> source, Func<TItem, TArg, CancellationToken, ValueTask<TResult>> selector, TArg arg, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<TResult>.GetInstance();

            foreach (var item in source)
            {
                builder.Add(await selector(item, arg, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
        }

        public static async ValueTask<ImmutableArray<TResult>> SelectManyAsArrayAsync<TItem, TArg, TResult>(this IEnumerable<TItem> source, Func<TItem, TArg, CancellationToken, ValueTask<IEnumerable<TResult>>> selector, TArg arg, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<TResult>.GetInstance();

            foreach (var item in source)
            {
                builder.AddRange(await selector(item, arg, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
        }

        public static async ValueTask<IEnumerable<TResult>> SelectManyInParallelAsync<TItem, TResult>(
           this IEnumerable<TItem> sequence,
           Func<TItem, CancellationToken, Task<IEnumerable<TResult>>> selector,
           CancellationToken cancellationToken)
        {
            return (await Task.WhenAll(sequence.Select(item => selector(item, cancellationToken))).ConfigureAwait(false)).Flatten();
        }

        public static bool All(this IEnumerable<bool> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (var b in source)
            {
                if (!b)
                {
                    return false;
                }
            }

            return true;
        }

        public static int IndexOf<T>(this IEnumerable<T> sequence, T value)
        {
            return sequence switch
            {
                IList<T> list => list.IndexOf(value),
                IReadOnlyList<T> readOnlyList => IndexOf(readOnlyList, value, EqualityComparer<T>.Default),
                _ => EnumeratingIndexOf(sequence, value, EqualityComparer<T>.Default)
            };
        }

        public static int IndexOf<T>(this IEnumerable<T> sequence, T value, IEqualityComparer<T> comparer)
        {
            return sequence switch
            {
                IReadOnlyList<T> readOnlyList => IndexOf(readOnlyList, value, comparer),
                _ => EnumeratingIndexOf(sequence, value, comparer)
            };
        }

        private static int EnumeratingIndexOf<T>(this IEnumerable<T> sequence, T value, IEqualityComparer<T> comparer)
        {
            int i = 0;
            foreach (var item in sequence)
            {
                if (comparer.Equals(item, value))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer<T> comparer)
        {
            for (int i = 0, length = list.Count; i < length; i++)
            {
                if (comparer.Equals(list[i], value))
                {
                    return i;
                }
            }

            return -1;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            return sequence.SelectMany(s => s);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IComparer<T>? comparer)
        {
            return source.OrderBy(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source, IComparer<T>? comparer)
        {
            return source.OrderByDescending(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            return source.OrderBy(Comparer<T>.Create(compare));
        }

        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            return source.OrderByDescending(Comparer<T>.Create(compare));
        }

#if NET7_0_OR_GREATER
        public static IOrderedEnumerable<T> Order<T>(IEnumerable<T> source) where T : IComparable<T>
#else
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) where T : IComparable<T>
#endif
        {
            return source.OrderBy(Comparer<T>.Default);
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source, IComparer<T>? comparer)
        {
            return source.ThenBy(Functions<T>.Identity, comparer);
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source, Comparison<T> compare)
        {
            return source.ThenBy(Comparer<T>.Create(compare));
        }

        public static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> source) where T : IComparable<T>
        {
            return source.ThenBy(Comparer<T>.Default);
        }

        public static bool IsSorted<T>(this IEnumerable<T> enumerable, IComparer<T>? comparer = null)
        {
            using var e = enumerable.GetEnumerator();
            if (!e.MoveNext())
            {
                return true;
            }

            comparer ??= Comparer<T>.Default;

            var previous = e.Current;
            while (e.MoveNext())
            {
                if (comparer.Compare(previous, e.Current) > 0)
                {
                    return false;
                }

                previous = e.Current;
            }

            return true;
        }

        public static bool Contains<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            return sequence.Any(predicate);
        }

        public static bool Contains(this IEnumerable<string?> sequence, string? s)
        {
            foreach (var item in sequence)
            {
                if (item == s)
                {
                    return true;
                }
            }

            return false;
        }

        public static IComparer<T> ToComparer<T>(this Comparison<T> comparison)
        {
            return Comparer<T>.Create(comparison);
        }

        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>>? items)
            where K : notnull
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>();
            }

            return ImmutableDictionary.CreateRange(items);
        }

        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>>? items, IEqualityComparer<K>? keyComparer)
            where K : notnull
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>(keyComparer);
            }

            return ImmutableDictionary.CreateRange(keyComparer, items);
        }

#nullable disable // Transpose doesn't handle empty arrays. Needs to be updated as appropriate.
        internal static IList<IList<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> data)
        {
#if DEBUG
            var count = data.First().Count();
            Debug.Assert(data.All(d => d.Count() == count));
#endif
            return TransposeInternal(data).ToArray();
        }

        private static IEnumerable<IList<T>> TransposeInternal<T>(this IEnumerable<IEnumerable<T>> data)
        {
            List<IEnumerator<T>> enumerators = new List<IEnumerator<T>>();

            var width = 0;
            foreach (var e in data)
            {
                enumerators.Add(e.GetEnumerator());
                width += 1;
            }

            try
            {
                while (true)
                {
                    T[] line = null;
                    for (int i = 0; i < width; i++)
                    {
                        var e = enumerators[i];
                        if (!e.MoveNext())
                        {
                            yield break;
                        }

                        if (line == null)
                        {
                            line = new T[width];
                        }

                        line[i] = e.Current;
                    }

                    yield return line;
                }
            }
            finally
            {
                foreach (var enumerator in enumerators)
                {
                    enumerator.Dispose();
                }
            }
        }
#nullable enable

        internal static Dictionary<K, ImmutableArray<T>> ToMultiDictionary<K, T>(this IEnumerable<T> data, Func<T, K> keySelector, IEqualityComparer<K>? comparer = null)
            where K : notnull
        {
            var dictionary = new Dictionary<K, ImmutableArray<T>>(comparer);
            var groups = data.GroupBy(keySelector, comparer);
            foreach (var grouping in groups)
            {
                var items = grouping.AsImmutable();
                dictionary.Add(grouping.Key, items);
            }

            return dictionary;
        }

        /// <summary>
        /// Returns the only element of specified sequence if it has exactly one, and default(TSource) otherwise.
        /// Unlike <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})"/> doesn't throw if there is more than one element in the sequence.
        /// </summary>
        internal static TSource? AsSingleton<TSource>(this IEnumerable<TSource>? source)
        {
            if (source == null)
            {
                return default;
            }

            if (source is IList<TSource> list)
            {
                return (list.Count == 1) ? list[0] : default;
            }

            using IEnumerator<TSource> e = source.GetEnumerator();
            if (!e.MoveNext())
            {
                return default;
            }

            TSource result = e.Current;
            if (e.MoveNext())
            {
                return default;
            }

            return result;
        }
    }

    /// <summary>
    /// Cached versions of commonly used delegates.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class Functions<T>
    {
        public static readonly Func<T, T> Identity = t => t;
        public static readonly Func<T, bool> True = t => true;
    }

    /// <summary>
    /// Cached versions of commonly used delegates.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class Predicates<T>
    {
        public static readonly Predicate<T> True = t => true;
    }
}

namespace System.Linq
{
    /// <summary>
    /// Declare the following extension methods in System.Linq namespace to avoid accidental boxing of ImmutableArray{T} that implements IEnumerable{T}.
    /// The boxing would occur if the methods were defined in Roslyn.Utilities and the file calling these methods has <c>using Roslyn.Utilities</c>
    /// but not <c>using System.Linq</c>.
    /// </summary>
    internal static class EnumerableExtensions
    {
        public static bool SequenceEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second, Func<T, T, bool> comparer)
        {
            RoslynDebug.Assert(comparer != null);

            if (first == second)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            using (var enumerator = first.GetEnumerator())
            using (var enumerator2 = second.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!enumerator2.MoveNext() || !comparer(enumerator.Current, enumerator2.Current))
                    {
                        return false;
                    }
                }

                if (enumerator2.MoveNext())
                {
                    return false;
                }
            }

            return true;
        }

        public static T? AggregateOrDefault<T>(this IEnumerable<T> source, Func<T, T, T> func)
        {
            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return default;
                }

                var result = e.Current;
                while (e.MoveNext())
                {
                    result = func(result, e.Current);
                }

                return result;
            }
        }
    }
}
