// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Provides helpers for asynchronous lazy initialization similar to <see cref="LazyInitializer"/>.
    /// </summary>
    internal static class AsyncLazyInitializer
    {
        public delegate ref T? Accessor<T>();

        public delegate ref T? Accessor<T, TState>(TState state);

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function if it hasn't already been
        /// initialized.
        /// </summary>
        /// <remarks>
        /// <para>This method may only be used on reference types, and <paramref name="valueFactory"/> may not return
        /// <see langword="null"/>.</para>
        ///
        /// <para>This method may be used concurrently by multiple threads to initialize the target returned by
        /// <paramref name="targetAccessor"/>. In the event that multiple threads access this method concurrently,
        /// multiple instances of <typeparamref name="T"/> may be created, but only one will be stored into
        /// the target. In such an occurrence, this method will not dispose of the objects that were not stored. If such
        /// objects must be disposed, it is up to the caller to determine if an object was not used and to then dispose
        /// of the object appropriately.</para>
        /// </remarks>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize if it hasn't already been initialized.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="targetAccessor"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If the target value is not already initialized and <paramref name="valueFactory"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="valueFactory"/> returned <see langword="null"/>.</exception>
        public static ValueTask<T> EnsureInitializedAsync<T>(Accessor<T> targetAccessor, Func<ValueTask<T>> valueFactory)
            where T : class
        {
            _ = targetAccessor ?? throw new ArgumentNullException(nameof(targetAccessor));

            // Fast path
            var value = Volatile.Read(ref targetAccessor());
            if (value is not null)
            {
                return new ValueTask<T>(value);
            }

            return EnsureInitializedCoreAsync(targetAccessor, valueFactory, null);
        }

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function if it hasn't already been
        /// initialized.
        /// </summary>
        /// <remarks>
        /// <para>This method may only be used on reference types, and <paramref name="valueFactory"/> may not return
        /// <see langword="null"/>.</para>
        ///
        /// <para>This method may be used concurrently by multiple threads to initialize the target returned by
        /// <paramref name="targetAccessor"/>. In the event that multiple threads access this method concurrently,
        /// multiple instances of <typeparamref name="T"/> may be created, but only one will be stored into
        /// the target. If the value computed by this method is not used, <paramref name="releaseUnusedValue"/> will be
        /// called to perform any necessary cleanup operations.</para>
        /// </remarks>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize if it hasn't already been initialized.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <param name="releaseUnusedValue">The action invoked to release a reference which was computed but not used.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="targetAccessor"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If the target value is not already initialized and <paramref name="valueFactory"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="valueFactory"/> returned <see langword="null"/>.</exception>
        public static ValueTask<T> EnsureInitializedAsync<T>(Accessor<T> targetAccessor, Func<ValueTask<T>> valueFactory, Action<T>? releaseUnusedValue)
            where T : class
        {
            _ = targetAccessor ?? throw new ArgumentNullException(nameof(targetAccessor));

            // Fast path
            var value = Volatile.Read(ref targetAccessor());
            if (value is not null)
            {
                return new ValueTask<T>(value);
            }

            return EnsureInitializedCoreAsync(targetAccessor, valueFactory, releaseUnusedValue);
        }

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function if it hasn't already been
        /// initialized.
        /// </summary>
        /// <remarks>
        /// <para>This method may only be used on reference types, and <paramref name="valueFactory"/> may not return
        /// <see langword="null"/>.</para>
        ///
        /// <para>This method may be used concurrently by multiple threads to initialize the target returned by
        /// <paramref name="targetAccessor"/>. In the event that multiple threads access this method concurrently,
        /// multiple instances of <typeparamref name="T"/> may be created, but only one will be stored into
        /// the target. In such an occurrence, this method will not dispose of the objects that were not stored. If such
        /// objects must be disposed, it is up to the caller to determine if an object was not used and to then dispose
        /// of the object appropriately.</para>
        /// </remarks>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <typeparam name="TState">The type of the state object to pass to the accessor and value factory.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize if it hasn't already been initialized.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <param name="state">The state object to pass to the accessor and value factory.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="targetAccessor"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If the target value is not already initialized and <paramref name="valueFactory"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="valueFactory"/> returned <see langword="null"/>.</exception>
        public static ValueTask<T> EnsureInitializedAsync<T, TState>(Accessor<T, TState> targetAccessor, Func<TState, ValueTask<T>> valueFactory, TState state)
            where T : class
        {
            _ = targetAccessor ?? throw new ArgumentNullException(nameof(targetAccessor));

            // Fast path
            var value = Volatile.Read(ref targetAccessor(state));
            if (value is not null)
            {
                return new ValueTask<T>(value);
            }

            return EnsureInitializedCoreAsync(targetAccessor, valueFactory, null, state);
        }

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function if it hasn't already been
        /// initialized.
        /// </summary>
        /// <remarks>
        /// <para>This method may only be used on reference types, and <paramref name="valueFactory"/> may not return
        /// <see langword="null"/>.</para>
        ///
        /// <para>This method may be used concurrently by multiple threads to initialize the target returned by
        /// <paramref name="targetAccessor"/>. In the event that multiple threads access this method concurrently,
        /// multiple instances of <typeparamref name="T"/> may be created, but only one will be stored into
        /// the target. If the value computed by this method is not used, <paramref name="releaseUnusedValue"/> will be
        /// called to perform any necessary cleanup operations.</para>
        /// </remarks>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <typeparam name="TState">The type of the state object to pass to the accessor and value factory.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize if it hasn't already been initialized.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <param name="releaseUnusedValue">The action invoked to release a reference which was computed but not used.</param>
        /// <param name="state">The state object to pass to the accessor, value factory, and cleanup action.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="targetAccessor"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If the target value is not already initialized and <paramref name="valueFactory"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="valueFactory"/> returned <see langword="null"/>.</exception>
        public static ValueTask<T> EnsureInitializedAsync<T, TState>(Accessor<T, TState> targetAccessor, Func<TState, ValueTask<T>> valueFactory, Action<T, TState>? releaseUnusedValue, TState state)
            where T : class
        {
            _ = targetAccessor ?? throw new ArgumentNullException(nameof(targetAccessor));

            // Fast path
            var value = Volatile.Read(ref targetAccessor(state));
            if (value is not null)
            {
                return new ValueTask<T>(value);
            }

            return EnsureInitializedCoreAsync(targetAccessor, valueFactory, releaseUnusedValue, state);
        }

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function (slow path).
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <param name="releaseUnusedValue">The action invoked to release a reference which was computed but not used.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        private static async ValueTask<T> EnsureInitializedCoreAsync<T>(Accessor<T> targetAccessor, Func<ValueTask<T>> valueFactory, Action<T>? releaseUnusedValue)
            where T : class
        {
            _ = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));

            var value = await valueFactory().ConfigureAwait(false);
            if (value is null)
            {
                throw new InvalidOperationException(WorkspacesResources.AsyncLazy_StaticInit_InvalidOperation);
            }

            var existingValue = Interlocked.CompareExchange(ref targetAccessor(), value, null);
            try
            {
                return existingValue ?? value;
            }
            finally
            {
                if (existingValue != null && existingValue != value)
                {
                    // If necessary, release the value that was computed but not used
                    releaseUnusedValue?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Initializes a target reference type by using a specified asynchronous function (slow path).
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <typeparam name="TState">The type of the state object to pass to the accessor and value factory.</typeparam>
        /// <param name="targetAccessor">An accessor that provides a reference of type <typeparamref name="T"/> to initialize.</param>
        /// <param name="valueFactory">The function that is called to initialize the reference.</param>
        /// <param name="releaseUnusedValue">The action invoked to release a reference which was computed but not used.</param>
        /// <param name="state">The state object to pass to the accessor and value factory.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        private static async ValueTask<T> EnsureInitializedCoreAsync<T, TState>(Accessor<T, TState> targetAccessor, Func<TState, ValueTask<T>> valueFactory, Action<T, TState>? releaseUnusedValue, TState state)
            where T : class
        {
            _ = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));

            var value = await valueFactory(state).ConfigureAwait(false);
            if (value is null)
            {
                throw new InvalidOperationException(WorkspacesResources.AsyncLazy_StaticInit_InvalidOperation);
            }

            var existingValue = Interlocked.CompareExchange(ref targetAccessor(state), value, null);
            try
            {
                return existingValue ?? value;
            }
            finally
            {
                if (existingValue != null && existingValue != value)
                {
                    // If necessary, release the value that was computed but not used
                    releaseUnusedValue?.Invoke(value, state);
                }
            }
        }
    }
}
