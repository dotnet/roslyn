// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SQLite
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
        /// </summary>
        private readonly ConcurrentExclusiveSchedulerPair _readerWriterLock = new ConcurrentExclusiveSchedulerPair();

        private Task<T> PerformReadAsync<T>(Func<T> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ConcurrentScheduler);

        private Task<T> PerformWriteAsync<T>(Func<T> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ExclusiveScheduler);
    }
}
