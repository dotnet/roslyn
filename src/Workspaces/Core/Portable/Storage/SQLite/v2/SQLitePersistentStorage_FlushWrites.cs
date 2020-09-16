// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// A queue to batch up flush requests and ensure that we don't issue then more often than every <see
        /// cref="FlushAllDelayMS"/>.
        /// </summary>
        private readonly AsyncBatchingDelay _flushQueue;

        private void EnqueueFlushTask()
        {
            _flushQueue.RequeueWork();
        }

        private Task FlushInMemoryDataToDiskIfNotShutdownAsync(CancellationToken cancellationToken)
        {
            // When we are asked to flush, go actually acquire the write-scheduler and perform the actual writes from
            // it. Note: this is only called max every FlushAllDelayMS.  So we don't bother trying to avoid the delegate
            // allocation here.
            return this.PerformWriteAsync(FlushInMemoryDataToDisk, cancellationToken);
        }

        private void FlushWritesOnClose()
        {
            // Issue a write task to write this all out to disk.
            //
            // Note: this only happens on close, so we don't try to avoid allocations here.

            var writeTask = PerformWriteAsync(
                () =>
                {
                    // Perform the actual write while having exclusive access to the scheduler.
                    FlushInMemoryDataToDisk();

                    // Now that we've done this, definitely cancel any further work. From this point on, it is now
                    // invalid for any codepaths to try to acquire a db connection for any purpose (beyond us
                    // disposing things below).
                    //
                    // This will also ensure that if we have a bg flush task still pending, when it wakes up it will
                    // see that we're shutdown and not proceed (and importantly won't acquire a connection). Because
                    // both the bg task and us run serialized, there is no way for it to miss this token
                    // cancellation.  If it runs after us, then it sees this.  If it runs before us, then we just
                    // block until it finishes.
                    //
                    // We don't have to worry about reads/writes getting connections either.  
                    // The only way we can get disposed in the first place is if every user of this storage instance
                    // has released their ref on us. In that case, it would be an error on their part to ever try to
                    // read/write after releasing us.
                    _shutdownTokenSource.Cancel();
                }, CancellationToken.None);

            // Wait for that task to finish.
            writeTask.Wait();
        }

        private void FlushInMemoryDataToDisk()
        {
            // We're writing.  This better always be under the exclusive scheduler.
            Debug.Assert(TaskScheduler.Current == _readerWriterLock.ExclusiveScheduler);

            // Don't flush from a bg task if we've been asked to shutdown.  The shutdown logic in the storage service
            // will take care of the final writes to the main db.
            if (_shutdownTokenSource.IsCancellationRequested)
                return;

            using var _ = GetPooledConnection(out var connection);

            // Dummy value for RunInTransaction signature.
            var unused = true;
            connection.RunInTransaction(_ =>
            {
                _solutionAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(connection);
                _projectAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(connection);
                _documentAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(connection);
            }, unused);
        }
    }
}
