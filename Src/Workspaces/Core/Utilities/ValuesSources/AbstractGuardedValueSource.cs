using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A value source that guards access to its value so only one computation 
    /// happens at a time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class AbstractGuardedValueSource<T> : ValueSource<T>
    {
        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            var @lock = this.GetLock();
            using (@lock.DisposableWait(cancellationToken))
            {
                return this.TranslateGuardedValue(this.GetGuardedValue(cancellationToken));
            }
        }

        protected abstract T GetGuardedValue(CancellationToken cancellationToken);

        public override Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAsyncLazy().GetValueAsync(cancellationToken);
        }

        protected abstract Task<T> GetGuardedValueAsync(CancellationToken cancellationToken);

        protected virtual T TranslateGuardedValue(T value)
        {
            return value;
        }

        private static readonly ConditionalWeakTable<object, NonReentrantLock> locks =
            new ConditionalWeakTable<object, NonReentrantLock>();

        internal NonReentrantLock GetLock()
        {
            NonReentrantLock @lock;
            if (!locks.TryGetValue(this, out @lock))
            {
                @lock = locks.GetValue(this, _ => new NonReentrantLock());
            }

            return @lock;
        }

        private AsyncLazy<T> asyncLazy;

        private AsyncLazy<T> GetAsyncLazy()
        {
            if (asyncLazy == null)
            {
                var newAsyncLazy = new AsyncLazy<T>(c => this.GetGuardedValueAsync(c).SafeContinueWith(task =>
                {
                    var @lock = this.GetLock();
                    using (@lock.DisposableWait(c))
                    {
                        return this.TranslateGuardedValue(task.Result);
                    }
                }, c, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default), cacheResult: false);

                Interlocked.CompareExchange(ref asyncLazy, newAsyncLazy, null);
            }

            return asyncLazy;
        }
    }
}