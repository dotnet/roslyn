// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Use a <see cref="ConcurrentExclusiveSchedulerPair"/> to simulate a reader-writer lock.
        /// Read operations are performed on the <see cref="ConcurrentExclusiveSchedulerPair.ConcurrentScheduler"/>
        /// and writes are performed on the <see cref="ConcurrentExclusiveSchedulerPair.ExclusiveScheduler"/>.
        ///
        /// We use this as a condition of using the in-memory shared-cache sqlite DB.  This DB
        /// doesn't busy-wait when attempts are made to lock the tables in it, which can lead to
        /// deadlocks.  Specifically, consider two threads doing the following:
        ///
        /// Thread A starts a transaction that starts as a reader, and later attempts to perform a
        /// write. Thread B is a writer (either started that way, or started as a reader and
        /// promoted to a writer first). B holds a RESERVED lock, waiting for readers to clear so it
        /// can start writing. A holds a SHARED lock (it's a reader) and tries to acquire RESERVED
        /// lock (so it can start writing).  The only way to make progress in this situation is for
        /// one of the transactions to roll back. No amount of waiting will help, so when SQLite
        /// detects this situation, it doesn't honor the busy timeout.
        ///
        /// To prevent this scenario, we control our access to the db explicitly with operations that
        /// can concurrently read, and operations that exclusively write.
        /// 
        /// All code that reads or writes from the db should go through this.
        /// </summary>
        private readonly ConcurrentExclusiveSchedulerPair _readerWriterLock = new ConcurrentExclusiveSchedulerPair();

        private static async Task<TResult> PerformTaskAsync<TArg, TResult>(
            Func<TArg, TResult> func, TArg arg,
            TaskScheduler scheduler, CancellationToken cancellationToken) where TArg : struct
        {
            // Get a pooled delegate that can be used to prevent having to alloc a new lambda that calls 'func' while
            // capturing 'arg'.  This is needed as Task.Factory.StartNew has no way to pass extra data around with it
            // except by boxing it as an object.
            using var _ = PooledDelegates.GetPooledFunction(func, arg, out var boundFunction);

            var task = Task.Factory.StartNew(boundFunction, cancellationToken, TaskCreationOptions.None, scheduler);

            return await task.ConfigureAwait(false);
        }

        // Read tasks go to the concurrent-scheduler where they can run concurrently with other read
        // tasks.
        private Task<TResult> PerformReadAsync<TArg, TResult>(Func<TArg, TResult> func, TArg arg, CancellationToken cancellationToken) where TArg : struct
            => PerformTaskAsync(func, arg, _readerWriterLock.ConcurrentScheduler, cancellationToken);

        // Write tasks go to the exclusive-scheduler so they run exclusively of all other threading
        // tasks we need to do.
        public Task<bool> PerformWriteAsync<TArg>(Func<TArg, bool> func, TArg arg, CancellationToken cancellationToken) where TArg : struct
            => PerformTaskAsync(func, arg, _readerWriterLock.ExclusiveScheduler, cancellationToken);

        public Task PerformWriteAsync(Action action, CancellationToken cancellationToken)
            => PerformWriteAsync(vt =>
            {
                vt.Item1();
                return true;
            }, ValueTuple.Create(action), cancellationToken);
    }
}
