// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class InterlockedOperations
    {
        /// <summary>
        /// Initialize the value referenced by <paramref name="target"/> in a thread-safe manner.
        /// The value is changed to <paramref name="value"/> only if the current value is null.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="target">Reference to the target location.</param>
        /// <param name="value">The value to use if the target is currently null.</param>
        /// <returns>The new value referenced by <paramref name="target"/>. Note that this is
        /// nearly always more useful than the usual return from <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>
        /// because it saves another read to <paramref name="target"/>.</returns>
        public static T Initialize<T>([NotNull] ref T? target, T value) where T : class
        {
            RoslynDebug.Assert((object?)value != null);
            return Interlocked.CompareExchange(ref target, value, null) ?? value;
        }

        /// <summary>
        /// Initialize the value referenced by <paramref name="target"/> in a thread-safe manner.
        /// The value is changed to <paramref name="initializedValue"/> only if the current value
        /// is <paramref name="uninitializedValue"/>.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="target">Reference to the target location.</param>
        /// <param name="initializedValue">The value to use if the target is currently uninitialized.</param>
        /// <param name="uninitializedValue">The uninitialized value.</param>
        /// <returns>The new value referenced by <paramref name="target"/>. Note that this is
        /// nearly always more useful than the usual return from <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>
        /// because it saves another read to <paramref name="target"/>.</returns>
        [return: NotNullIfNotNull(parameterName: nameof(initializedValue))]
        public static T Initialize<T>(ref T target, T initializedValue, T uninitializedValue) where T : class?
        {
            Debug.Assert((object?)initializedValue != uninitializedValue);
            T oldValue = Interlocked.CompareExchange(ref target, initializedValue, uninitializedValue);
            return (object?)oldValue == uninitializedValue ? initializedValue : oldValue;
        }

        /// <summary>
        /// Initialize the immutable array referenced by <paramref name="target"/> in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">Elemental type of the array.</typeparam>
        /// <param name="target">Reference to the target location.</param>
        /// <param name="initializedValue">The value to use if the target is currently uninitialized (default).</param>
        /// <returns>The new value referenced by <paramref name="target"/>. Note that this is
        /// nearly always more useful than the usual return from <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>
        /// because it saves another read to <paramref name="target"/>.</returns>
        public static ImmutableArray<T> Initialize<T>(ref ImmutableArray<T> target, ImmutableArray<T> initializedValue)
        {
            Debug.Assert(!initializedValue.IsDefault);
            var oldValue = ImmutableInterlocked.InterlockedCompareExchange(ref target, initializedValue, default(ImmutableArray<T>));
            return oldValue.IsDefault ? initializedValue : oldValue;
        }

        /// <summary>
        /// Initialize the immutable array referenced by <paramref name="target"/> in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">Elemental type of the array.</typeparam>
        /// <param name="createArray">Callback to produce the array if <paramref name="target"/> is 'default'.  May be
        /// called multiple times in the event of concurrent initialization of <paramref name="target"/>.  Will not be
        /// called if 'target' is already not 'default' at the time this is called.</param>
        /// <returns>The value of <paramref name="target"/> after initialization.  If <paramref name="target"/> is
        /// already initialized, that value value will be returned.</returns>
        public static ImmutableArray<T> InterlockedInitialize<T>(ref ImmutableArray<T> target, Func<ImmutableArray<T>> createArray)
            => InterlockedInitialize(ref target, static createArray => createArray(), createArray);

        public static ImmutableArray<T> InterlockedInitialize<T, TArg>(ref ImmutableArray<T> target, Func<TArg, ImmutableArray<T>> createArray, TArg arg)
        {
            if (!target.IsDefault)
            {
                return target;
            }

            return InterlockedInitialize_Slow(ref target, createArray, arg);
        }

        private static ImmutableArray<T> InterlockedInitialize_Slow<T, TArg>(ref ImmutableArray<T> target, Func<TArg, ImmutableArray<T>> createArray, TArg arg)
        {
            ImmutableInterlocked.Update(
                ref target,
                static (current, tuple) =>
                {
                    // Once initialized, never reinitialize.
                    if (!current.IsDefault)
                        return current;

                    return tuple.createArray(tuple.arg);
                }, (createArray, arg));

            return target;
        }
    }
}
