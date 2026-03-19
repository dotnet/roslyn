// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class RoslynLazyInitializer
    {
        /// <inheritdoc cref="LazyInitializer.EnsureInitialized{T}(ref T)"/>
        public static T EnsureInitialized<T>([NotNull] ref T? target) where T : class
            => LazyInitializer.EnsureInitialized<T>(ref target!);

        /// <inheritdoc cref="LazyInitializer.EnsureInitialized{T}(ref T, Func{T})"/>
        public static T EnsureInitialized<T>([NotNull] ref T? target, Func<T> valueFactory) where T : class
            => LazyInitializer.EnsureInitialized<T>(ref target!, valueFactory);

        /// <inheritdoc cref="LazyInitializer.EnsureInitialized{T}(ref T, ref bool, ref object)"/>
        public static T EnsureInitialized<T>([NotNull] ref T? target, ref bool initialized, [NotNullIfNotNull(nameof(syncLock))] ref object? syncLock)
            => LazyInitializer.EnsureInitialized<T>(ref target!, ref initialized, ref syncLock);

        /// <inheritdoc cref="LazyInitializer.EnsureInitialized{T}(ref T, ref bool, ref object, Func{T})"/>
        public static T EnsureInitialized<T>([NotNull] ref T? target, ref bool initialized, [NotNullIfNotNull(nameof(syncLock))] ref object? syncLock, Func<T> valueFactory)
            => LazyInitializer.EnsureInitialized<T>(ref target!, ref initialized, ref syncLock, valueFactory);
    }
}
