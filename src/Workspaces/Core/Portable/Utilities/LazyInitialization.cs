// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class LazyInitialization
    {
        internal static T InterlockedStore<T>(ref T target, T value) where T : class
        {
            Debug.Assert(value != null);
            return Interlocked.CompareExchange(ref target, value, null) ?? value;
        }

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of the target value. Must be a reference type.</typeparam>
        /// <param name="target">The target to initialize.</param>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <returns>The target value.</returns>
        public static T EnsureInitialized<T>(ref T target, Func<T> valueFactory) where T : class
        {
            return Volatile.Read(ref target) ?? InterlockedStore(ref target, valueFactory());
        }

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <typeparam name="T">The type of the target value. Must be a reference type.</typeparam>
        /// <param name="target">The target to initialize.</param>
        /// <typeparam name="U">The type of the <paramref name="state"/> argument passed to the value factory.</typeparam>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <param name="state">An argument passed to the value factory.</param>
        /// <returns>The target value.</returns>
        public static T EnsureInitialized<T, U>(ref T target, Func<U, T> valueFactory, U state)
            where T : class
        {
            return Volatile.Read(ref target) ?? InterlockedStore(ref target, valueFactory(state));
        }
    }
}
