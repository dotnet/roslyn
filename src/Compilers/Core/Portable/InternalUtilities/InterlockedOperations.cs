// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class InterlockedOperations
    {
        private static T GetOrStore<T>([NotNull] ref T? target, T value) where T : class
            => Interlocked.CompareExchange(ref target, value, null) ?? value;

        private static int GetOrStore(ref int target, int value, int uninitializedValue)
        {
            var existingValue = Interlocked.CompareExchange(ref target, value, uninitializedValue);
            return existingValue == uninitializedValue ? value : existingValue;
        }

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of the target value. Must be a reference type.</typeparam>
        /// <param name="target">The target to initialize.</param>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <returns>The target value.</returns>
        public static T Initialize<T>([NotNull] ref T? target, Func<T> valueFactory) where T : class
            => Volatile.Read(ref target!) ?? GetOrStore(ref target, valueFactory());

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of the target value. Must be a reference type.</typeparam>
        /// <param name="target">The target to initialize.</param>
        /// <typeparam name="TArg">The type of the <paramref name="arg"/> argument passed to the value factory.</typeparam>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <param name="arg">An argument passed to the value factory.</param>
        /// <returns>The target value.</returns>
        public static T Initialize<T, TArg>([NotNull] ref T? target, Func<TArg, T> valueFactory, TArg arg)
            where T : class
        {
            return Volatile.Read(ref target!) ?? GetOrStore(ref target, valueFactory(arg));
        }

        /// <summary>
        /// Ensure that the given target value is initialized in a thread-safe manner.
        /// </summary>
        /// <param name="target">The target to initialize.</param>
        /// <param name="uninitializedValue">The value indicating <paramref name="target"/> is not yet initialized.</param>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <param name="arg">An argument passed to the value factory.</param>
        /// <typeparam name="TArg">The type of the <paramref name="arg"/> argument passed to the value factory.</typeparam>
        /// <remarks>
        /// If <paramref name="valueFactory"/> returns a value equal to <paramref name="uninitializedValue"/>, future
        /// calls to the same method may recalculate the target value.
        /// </remarks>
        /// <returns>The target value.</returns>
        public static int Initialize<TArg>(ref int target, int uninitializedValue, Func<TArg, int> valueFactory, TArg arg)
        {
            var existingValue = Volatile.Read(ref target);
            if (existingValue != uninitializedValue)
                return existingValue;

            return GetOrStore(ref target, valueFactory(arg), uninitializedValue);
        }

        /// <summary>
        /// Ensure that the given target value is initialized in a thread-safe manner. This overload supports the
        /// initialization of value types, and reference type fields where <see langword="null"/> is considered an
        /// initialized value.
        /// </summary>
        /// <typeparam name="T">The type of the target value.</typeparam>
        /// <param name="target">A target value box to initialize.</param>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <returns>The target value.</returns>
        public static T? Initialize<T>([NotNull] ref StrongBox<T?>? target, Func<T?> valueFactory)
        {
            var box = Volatile.Read(ref target!) ?? GetOrStore(ref target, new StrongBox<T?>(valueFactory()));
            return box.Value;
        }

        /// <summary>
        /// Ensure that the given target value is initialized in a thread-safe manner. This overload supports the
        /// initialization of value types, and reference type fields where <see langword="null"/> is considered an
        /// initialized value.
        /// </summary>
        /// <typeparam name="T">The type of the target value.</typeparam>
        /// <param name="target">A target value box to initialize.</param>
        /// <typeparam name="TArg">The type of the <paramref name="arg"/> argument passed to the value factory.</typeparam>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <param name="arg">An argument passed to the value factory.</param>
        /// <returns>The target value.</returns>
        public static T? Initialize<T, TArg>([NotNull] ref StrongBox<T?>? target, Func<TArg, T?> valueFactory, TArg arg)
        {
            var box = Volatile.Read(ref target!) ?? GetOrStore(ref target, new StrongBox<T?>(valueFactory(arg)));
            return box.Value;
        }

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
            return GetOrStore(ref target, value);
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
        public static ImmutableArray<T> Initialize<T>(ref ImmutableArray<T> target, Func<ImmutableArray<T>> createArray)
            => Initialize(ref target, static createArray => createArray(), createArray);

        /// <summary>
        /// Initialize the immutable array referenced by <paramref name="target"/> in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">Elemental type of the array.</typeparam>
        /// <typeparam name="TArg">The type of the <paramref name="arg"/> argument passed to the value factory.</typeparam>
        /// <param name="createArray">Callback to produce the array if <paramref name="target"/> is 'default'.  May be
        /// called multiple times in the event of concurrent initialization of <paramref name="target"/>.  Will not be
        /// called if 'target' is already not 'default' at the time this is called.</param>
        /// <returns>The value of <paramref name="target"/> after initialization.  If <paramref name="target"/> is
        /// already initialized, that value value will be returned.</returns>
        public static ImmutableArray<T> Initialize<T, TArg>(ref ImmutableArray<T> target, Func<TArg, ImmutableArray<T>> createArray, TArg arg)
        {
            if (!target.IsDefault)
            {
                return target;
            }

            return Initialize_Slow(ref target, createArray, arg);
        }

        private static ImmutableArray<T> Initialize_Slow<T, TArg>(ref ImmutableArray<T> target, Func<TArg, ImmutableArray<T>> createArray, TArg arg)
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
