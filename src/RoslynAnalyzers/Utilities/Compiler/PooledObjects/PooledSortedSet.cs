// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    /// <summary>
    /// Pooled <see cref="SortedSet{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of elements in the set.</typeparam>
    internal sealed class PooledSortedSet<T> : SortedSet<T>, IDisposable
    {
        private readonly ObjectPool<PooledSortedSet<T>>? _pool;

        public PooledSortedSet(ObjectPool<PooledSortedSet<T>>? pool, IComparer<T>? comparer = null)
            : base(comparer)
        {
            _pool = pool;
        }

        public void Dispose() => Free(CancellationToken.None);

        public void Free(CancellationToken cancellationToken)
        {
            // Do not free in presence of cancellation.
            // See https://github.com/dotnet/roslyn/issues/46859 for details.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledSortedSet<T>> s_poolInstance = CreatePool();
        private static readonly ConcurrentDictionary<IComparer<T>, ObjectPool<PooledSortedSet<T>>> s_poolInstancesByComparer = new();

        private static ObjectPool<PooledSortedSet<T>> CreatePool(IComparer<T>? comparer = null)
        {
            ObjectPool<PooledSortedSet<T>>? pool = null;
            pool = new ObjectPool<PooledSortedSet<T>>(
                () => new PooledSortedSet<T>(pool, comparer),
                128);
            return pool;
        }

        /// <summary>
        /// Gets a pooled instance of a <see cref="PooledSortedSet{T}"/> with an optional comparer.
        /// </summary>
        /// <param name="comparer">Singleton (or at least a bounded number) comparer to use, or null for the element type's default comparer.</param>
        /// <returns>An empty <see cref="PooledSortedSet{T}"/>.</returns>
        public static PooledSortedSet<T> GetInstance(IComparer<T>? comparer = null)
        {
            var pool = comparer == null ?
                s_poolInstance :
                s_poolInstancesByComparer.GetOrAdd(comparer, CreatePool);
            var instance = pool.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        /// <summary>
        /// Gets a pooled instance of a <see cref="PooledSortedSet{T}"/> with the given initializer and an optional comparer.
        /// </summary>
        /// <param name="initializer">Initializer for the set.</param>
        /// <param name="comparer">Comparer to use, or null for the element type's default comparer.</param>
        /// <returns>An empty <see cref="PooledSortedSet{T}"/>.</returns>
        public static PooledSortedSet<T> GetInstance(IEnumerable<T> initializer, IComparer<T>? comparer = null)
        {
            var instance = GetInstance(comparer);
            foreach (var value in initializer)
            {
                instance.Add(value);
            }

            return instance;
        }
    }
}
