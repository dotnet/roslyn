// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class RoslynImmutableInterlocked
    {
        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the list.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free,
        /// as it may run multiple times when races occur with other threads.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T>(ref ImmutableSegmentedList<T> location, Func<ImmutableSegmentedList<T>, ImmutableSegmentedList<T>> transformer)
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedList<T>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the list.</typeparam>
        /// <typeparam name="TArg">The type of argument passed to the <paramref name="transformer"/>.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free, as it may run multiple times
        /// when races occur with other threads.</param>
        /// <param name="transformerArgument">The argument to pass to <paramref name="transformer"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T, TArg>(ref ImmutableSegmentedList<T> location, Func<ImmutableSegmentedList<T>, TArg, ImmutableSegmentedList<T>> transformer, TArg transformerArgument)
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedList<T>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue, transformerArgument);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable list to the specified value and returns the previous
        /// value.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the list.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedList<T> InterlockedExchange<T>(ref ImmutableSegmentedList<T> location, ImmutableSegmentedList<T> value)
        {
            return ImmutableSegmentedList<T>.PrivateInterlocked.InterlockedExchange(ref location, value);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable list to the specified value if it is currently equal to
        /// another specified value. Returns the previous value.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the list.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="comparand">The value to check equality for before assigning.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedList<T> InterlockedCompareExchange<T>(ref ImmutableSegmentedList<T> location, ImmutableSegmentedList<T> value, ImmutableSegmentedList<T> comparand)
        {
            return ImmutableSegmentedList<T>.PrivateInterlocked.InterlockedCompareExchange(ref location, value, comparand);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable list to the specified value if it is has not yet been
        /// initialized.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the list.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns><see langword="true"/> if the field was assigned the specified value; otherwise,
        /// <see langword="false"/> if it was previously initialized.</returns>
        public static bool InterlockedInitialize<T>(ref ImmutableSegmentedList<T> location, ImmutableSegmentedList<T> value)
        {
            return InterlockedCompareExchange(ref location, value, default(ImmutableSegmentedList<T>)).IsDefault;
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the set.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free,
        /// as it may run multiple times when races occur with other threads.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T>(ref ImmutableSegmentedHashSet<T> location, Func<ImmutableSegmentedHashSet<T>, ImmutableSegmentedHashSet<T>> transformer)
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedHashSet<T>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the set.</typeparam>
        /// <typeparam name="TArg">The type of argument passed to the <paramref name="transformer"/>.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free, as it may run multiple times
        /// when races occur with other threads.</param>
        /// <param name="transformerArgument">The argument to pass to <paramref name="transformer"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T, TArg>(ref ImmutableSegmentedHashSet<T> location, Func<ImmutableSegmentedHashSet<T>, TArg, ImmutableSegmentedHashSet<T>> transformer, TArg transformerArgument)
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedHashSet<T>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue, transformerArgument);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable set to the specified value and returns the
        /// previous value.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the set.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedHashSet<T> InterlockedExchange<T>(ref ImmutableSegmentedHashSet<T> location, ImmutableSegmentedHashSet<T> value)
        {
            return ImmutableSegmentedHashSet<T>.PrivateInterlocked.InterlockedExchange(ref location, value);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable set to the specified value if it is currently
        /// equal to another specified value. Returns the previous value.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the set.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="comparand">The value to check equality for before assigning.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedHashSet<T> InterlockedCompareExchange<T>(ref ImmutableSegmentedHashSet<T> location, ImmutableSegmentedHashSet<T> value, ImmutableSegmentedHashSet<T> comparand)
        {
            return ImmutableSegmentedHashSet<T>.PrivateInterlocked.InterlockedCompareExchange(ref location, value, comparand);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable set to the specified value if it is has not yet
        /// been initialized.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the set.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns><see langword="true"/> if the field was assigned the specified value; otherwise,
        /// <see langword="false"/> if it was previously initialized.</returns>
        public static bool InterlockedInitialize<T>(ref ImmutableSegmentedHashSet<T> location, ImmutableSegmentedHashSet<T> value)
        {
            return InterlockedCompareExchange(ref location, value, default(ImmutableSegmentedHashSet<T>)).IsDefault;
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free,
        /// as it may run multiple times when races occur with other threads.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, Func<ImmutableSegmentedDictionary<TKey, TValue>, ImmutableSegmentedDictionary<TKey, TValue>> transformer)
            where TKey : notnull
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics via a specified transformation
        /// function. The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <typeparam name="TArg">The type of argument passed to the <paramref name="transformer"/>.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free, as it may run multiple times
        /// when races occur with other threads.</param>
        /// <param name="transformerArgument">The argument to pass to <paramref name="transformer"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the location's value is changed by applying the result of the
        /// <paramref name="transformer"/> function; otherwise, <see langword="false"/> if the location's value remained
        /// the same because the last invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<TKey, TValue, TArg>(ref ImmutableSegmentedDictionary<TKey, TValue> location, Func<ImmutableSegmentedDictionary<TKey, TValue>, TArg, ImmutableSegmentedDictionary<TKey, TValue>> transformer, TArg transformerArgument)
            where TKey : notnull
        {
            if (transformer is null)
                throw new ArgumentNullException(nameof(transformer));

            var oldValue = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                var newValue = transformer(oldValue, transformerArgument);
                if (oldValue == newValue)
                {
                    // No change was actually required.
                    return false;
                }

                var interlockedResult = InterlockedCompareExchange(ref location, newValue, oldValue);
                if (oldValue == interlockedResult)
                    return true;

                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable dictionary to the specified value and returns the
        /// previous value.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedDictionary<TKey, TValue> InterlockedExchange<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, ImmutableSegmentedDictionary<TKey, TValue> value)
            where TKey : notnull
        {
            return ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.InterlockedExchange(ref location, value);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable dictionary to the specified value if it is currently
        /// equal to another specified value. Returns the previous value.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="comparand">The value to check equality for before assigning.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableSegmentedDictionary<TKey, TValue> InterlockedCompareExchange<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, ImmutableSegmentedDictionary<TKey, TValue> value, ImmutableSegmentedDictionary<TKey, TValue> comparand)
            where TKey : notnull
        {
            return ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.InterlockedCompareExchange(ref location, value, comparand);
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable dictionary to the specified value if it is has not yet
        /// been initialized.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns><see langword="true"/> if the field was assigned the specified value; otherwise,
        /// <see langword="false"/> if it was previously initialized.</returns>
        public static bool InterlockedInitialize<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, ImmutableSegmentedDictionary<TKey, TValue> value)
            where TKey : notnull
        {
            return InterlockedCompareExchange(ref location, value, default(ImmutableSegmentedDictionary<TKey, TValue>)).IsDefault;
        }

        /// <inheritdoc cref="ImmutableInterlocked.GetOrAdd{TKey, TValue, TArg}(ref ImmutableDictionary{TKey, TValue}, TKey, Func{TKey, TArg, TValue}, TArg)"/>
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
            where TKey : notnull
        {
            if (valueFactory is null)
                throw new ArgumentNullException(nameof(valueFactory));

            var map = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            if (map.IsDefault)
                throw new ArgumentNullException(nameof(location));

            if (map.TryGetValue(key, out var value))
            {
                return value;
            }

            value = valueFactory(key, factoryArgument);
            return GetOrAdd(ref location, key, value);
        }

        /// <inheritdoc cref="ImmutableInterlocked.GetOrAdd{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, Func{TKey, TValue})"/>
        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> valueFactory)
            where TKey : notnull
        {
            if (valueFactory is null)
                throw new ArgumentNullException(nameof(valueFactory));

            var map = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            if (map.IsDefault)
                throw new ArgumentNullException(nameof(location));

            if (map.TryGetValue(key, out var value))
            {
                return value;
            }

            value = valueFactory(key);
            return GetOrAdd(ref location, key, value);
        }

        /// <inheritdoc cref="ImmutableInterlocked.GetOrAdd{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, TValue)"/>
        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, TValue value)
            where TKey : notnull
        {
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (priorCollection.TryGetValue(key, out var oldValue))
                {
                    return oldValue;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    // We won the race-condition and have updated the collection.
                    // Return the value that is in the collection (as of the Interlocked operation).
                    return value;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <inheritdoc cref="ImmutableInterlocked.AddOrUpdate{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, Func{TKey, TValue}, Func{TKey, TValue, TValue})"/>
        public static TValue AddOrUpdate<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            if (addValueFactory is null)
                throw new ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory is null)
                throw new ArgumentNullException(nameof(updateValueFactory));

            TValue newValue;
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (priorCollection.TryGetValue(key, out var oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                }
                else
                {
                    newValue = addValueFactory(key);
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    // We won the race-condition and have updated the collection.
                    // Return the value that is in the collection (as of the Interlocked operation).
                    return newValue;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <inheritdoc cref="ImmutableInterlocked.AddOrUpdate{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, TValue, Func{TKey, TValue, TValue})"/>
        public static TValue AddOrUpdate<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            if (updateValueFactory is null)
                throw new ArgumentNullException(nameof(updateValueFactory));

            TValue newValue;
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (priorCollection.TryGetValue(key, out var oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                }
                else
                {
                    newValue = addValue;
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    // We won the race-condition and have updated the collection.
                    // Return the value that is in the collection (as of the Interlocked operation).
                    return newValue;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <inheritdoc cref="ImmutableInterlocked.TryAdd{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, TValue)"/>
        public static bool TryAdd<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, TValue value)
            where TKey : notnull
        {
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (priorCollection.ContainsKey(key))
                {
                    return false;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    return true;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <inheritdoc cref="ImmutableInterlocked.TryUpdate{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, TValue, TValue)"/>
        public static bool TryUpdate<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, TValue newValue, TValue comparisonValue)
            where TKey : notnull
        {
            var valueComparer = EqualityComparer<TValue>.Default;
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (!priorCollection.TryGetValue(key, out var priorValue) || !valueComparer.Equals(priorValue, comparisonValue))
                {
                    // The key isn't in the dictionary, or its current value doesn't match what the caller expected.
                    return false;
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    return true;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }

        /// <inheritdoc cref="ImmutableInterlocked.TryRemove{TKey, TValue}(ref ImmutableDictionary{TKey, TValue}, TKey, out TValue)"/>
        public static bool TryRemove<TKey, TValue>(ref ImmutableSegmentedDictionary<TKey, TValue> location, TKey key, [MaybeNullWhen(false)] out TValue value)
            where TKey : notnull
        {
            var priorCollection = ImmutableSegmentedDictionary<TKey, TValue>.PrivateInterlocked.VolatileRead(in location);
            while (true)
            {
                if (priorCollection.IsDefault)
                    throw new ArgumentNullException(nameof(location));

                if (!priorCollection.TryGetValue(key, out value))
                {
                    return false;
                }

                var updatedCollection = priorCollection.Remove(key);
                var interlockedResult = InterlockedCompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    return true;
                }

                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
        }
    }
}
