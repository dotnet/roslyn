// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal sealed partial class SQLitePersistentStorage
{
    /// <summary>
    /// A queue to batch up flush requests and ensure that we don't issue then more often than every <see
    /// cref="FlushAllDelayMS"/>.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _flushQueue;

    private void EnqueueFlushTask()
    {
        _flushQueue.AddWork();
    }

    private async ValueTask FlushInMemoryDataToDiskIfNotShutdownAsync(CancellationToken cancellationToken)
    {
        // When we are asked to flush, go actually acquire the write-scheduler and perform the actual writes from
        // it. Note: this is only called max every FlushAllDelayMS.  So we don't bother trying to avoid the delegate
        // allocation here.
        await PerformWriteAsync(_flushInMemoryDataToDisk, cancellationToken).ConfigureAwait(false);
    }

    private void FlushInMemoryDataToDisk()
    {
        // We're writing.  This better always be under the exclusive scheduler.
        Contract.ThrowIfFalse(TaskScheduler.Current == this.Scheduler.ExclusiveScheduler);

        // Don't flush from a bg task if we've been asked to shutdown.  The shutdown logic in the storage service
        // will take care of the final writes to the main db.
        if (_shutdownTokenSource.IsCancellationRequested)
            return;

        using var _ = this.GetPooledConnection(out var connection);

        var exception = connection.RunInTransaction(static state =>
        {
            state.self._solutionAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(state.connection);
            state.self._projectAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(state.connection);
            state.self._documentAccessor.FlushInMemoryDataToDisk_MustRunInTransaction(state.connection);
        },
        (self: this, connection),
        throwOnSqlException: false);

        if (exception != null)
        {
            // Some sql exception occurred (like SQLITE_FULL). These are not exceptions we can suitably recover
            // from.  In this case, transition the storage instance into being unusable. Future reads/writes will
            // get empty results.
            this.DisableStorage(exception);
        }
    }
}
