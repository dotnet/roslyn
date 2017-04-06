// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Lock protecting the write queues and <see cref="_flushAllTask"/>.
        /// </summary>
        private readonly object _writeQueueGate = new object();

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB.
        /// </summary>
        private Task _flushAllTask;

        private void AddWriteTask<TKey>(MultiDictionary<TKey, Action<SqlConnection>> queue, TKey key, Action<SqlConnection> action)
        {
            lock (_writeQueueGate)
            {
                queue.Add(key, action);

                // If we don't have an outstanding request to write the queue to the DB
                // then create one to run a short while from now.  If there is an outstanding
                // request, then it will see this write request when it runs.
                if (_flushAllTask == null)
                {
                    var delay = 
                        Task.Delay(FlushAllDelayMS, _shutdownTokenSource.Token)
                            .ContinueWith(
                                _ => FlushAllPendingWrites(),
                                _shutdownTokenSource.Token,
                                TaskContinuationOptions.None,
                                TaskScheduler.Default);
                }
            }
        }

        private void FlushSpecificWrites<TKey>(
            SqlConnection connection,
            MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, CountdownEvent> keyToCountdown,
            TKey key)
        {
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                FlushSpecificWrites(connection, keyToWriteActions, keyToCountdown, key, writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void FlushSpecificWrites<TKey>(SqlConnection connection, MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions, Dictionary<TKey, CountdownEvent> keyToCountdown, TKey key, ArrayBuilder<Action<SqlConnection>> writesToProcess)
        {
            // Many threads many be trying to flush a specific queue.  If some other thread
            // beats us to writing this queue, we want to still wait until it is down.  To
            // accomplish that, we use a countdown that effectively states how many current
            // writers there are, and which only lets us past once all the concurrent writers
            // say they are done.
            CountdownEvent countdown;

            // Note: by blocking on _writeQueueGate we are guaranteed to see all the writes 
            // performed by FlushAllPendingWrites.
            lock (_writeQueueGate)
            {
                // Get the writes we need to process.
                writesToProcess.AddRange(keyToWriteActions[key]);

                // and clear them from the queues so we don't process things multiple times.
                keyToWriteActions.Remove(key);

                // We may have acquired _writeQueueGate between the time that an existing thread 
                // completes the "Wait" below and grabs this lock.  If that's the case, let go
                // of the countdown associated with this key as it is no longer usable.
                RemoveCountdownIfComplete(keyToCountdown, key);

                // See if there's an existing countdown keeping track of the number of writers
                // writing this queue.
                if (!keyToCountdown.TryGetValue(key, out countdown))
                {
                    // We're the first writer for this queue.  Set the count to one, and keep
                    // it around so future concurrent writers will see it.
                    countdown = new CountdownEvent(initialCount: 1);
                    keyToCountdown.Add(key, countdown);
                }
                else
                {
                    // If there is, increment the count to indicate that we're writing as well.
                    countdown.AddCount();
                }

                Debug.Assert(countdown.CurrentCount >= 1);
            }

            // Now actually process any writes we found for this queue.
            ProcessWriteQueue(connection, writesToProcess);

            // Mark that we're done writing out this queue, and wait until all other writers
            // for this queue are done.  Note: this needs to happen in the lock so that 
            // changes to the countdown value are observed consistently across all threads.
            bool lastSignal;
            lock (_writeQueueGate)
            {
                lastSignal = countdown.Signal();
            }

            // Don't proceed until all concurrent writers of this queue complete.
            countdown.Wait();

            // If we're the thread that finally got the countdown to zero, then dispose of this
            // count down and remove it from the dictionary (if it hasn't already been replaced
            // by the next request).
            if (lastSignal)
            {
                Debug.Assert(countdown.CurrentCount == 0);

                // Safe to call outside of lock.  Countdown is only given out to a set of threads
                // that have incremented it.  And we can only get here once all the threads have
                // been allowed to get past the 'Wait' point.  Only one of those threads will
                // have lastSignal set to true, so we'll only dispose this once.
                countdown.Dispose();

                lock (_writeQueueGate)
                {
                    // Remove the countdown if it's still in the dictionary.  It may not be if
                    // another thread came in after this batch of threads completed, and it 
                    // removed the completed countdown already.
                    RemoveCountdownIfComplete(keyToCountdown, key);
                }
            }
        }

        private void RemoveCountdownIfComplete<TKey>(
            Dictionary<TKey, CountdownEvent> keyToCountdown, TKey key)
        {
            Debug.Assert(Monitor.IsEntered(_writeQueueGate));

            if (keyToCountdown.TryGetValue(key, out var tempCountDown) &&
                tempCountDown.CurrentCount == 0)
            {
                keyToCountdown.Remove(key);
            }
        }

        private void FlushAllPendingWrites()
        {
            // Copy the work from _writeQueue to a local list that we can process.
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    // Copy the pending work the accessors have to the local copy.
                    _solutionAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _projectAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _documentAccessor.AddAndClearAllPendingWrites(writesToProcess);

                    // Indicate that there is no outstanding write task.  The next request to 
                    // write will cause one to be kicked off.
                    _flushAllTask = null;

                    // Note: we keep the lock while we're writing all.  That way if any reads come
                    // in and want to wait for the respective keys to be written, they will see the
                    // results of our writes after the lock is released.  Note: this is slightly
                    // heavyweight.  But as we're only doing these writes in bulk a couple of times
                    // a second max, this should not be an area of contention.
                    if (writesToProcess.Count > 0)
                    {
                        using (var pooledConnection = GetPooledConnection())
                        {
                            ProcessWriteQueue(pooledConnection.Connection, writesToProcess);
                        }
                    }
                }
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void ProcessWriteQueue(
            SqlConnection connection,
            ArrayBuilder<Action<SqlConnection>> writesToProcess)
        {
            if (writesToProcess.Count == 0)
            {
                return;
            }

            if (_shutdownTokenSource.Token.IsCancellationRequested)
            {
                // Don't actually try to perform any writes if we've been asked to shutdown.
                return;
            }

            try
            {
                // Create a transaction and perform all writes within it.
                connection.RunInTransaction(() =>
                {
                    foreach (var action in writesToProcess)
                    {
                        action(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }
        }
    }
}