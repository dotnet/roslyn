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
        /// </summary>
        private readonly ConcurrentExclusiveSchedulerPair _readerWriterLock = new ConcurrentExclusiveSchedulerPair();

        private Task<T> PerformReadAsync<T>(Func<T> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ConcurrentScheduler);

        private Task<T> PerformWriteAsync<T>(Func<T> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ExclusiveScheduler);
    }
}
