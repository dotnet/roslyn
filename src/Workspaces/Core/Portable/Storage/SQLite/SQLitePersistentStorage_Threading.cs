// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// 
        /// All code that reads or writes from the db should go through this.
        /// </summary>
        private readonly ConcurrentExclusiveSchedulerPair _readerWriterLock = new ConcurrentExclusiveSchedulerPair();

        // Inner class used so that we can generically create and pool StrongBox<TArg> instances. We
        // use those to provide an allocation-free means of calling
        // <c>Task.Factory.StartNew(func<object>, object, ...)</c>.
        private static class StrongBoxes<TArg>
            where TArg : struct
        {
            private static readonly Stack<StrongBox<TArg>> _boxes = new Stack<StrongBox<TArg>>();

            public static StrongBox<TArg> GetBox()
            {
                lock (_boxes)
                {
                    if (_boxes.Count > 0)
                    {
                        return _boxes.Pop();
                    }

                    return new StrongBox<TArg>();
                }
            }

            public static void ReturnBox(StrongBox<TArg> box)
            {
                lock (_boxes)
                {
                    _boxes.Push(box);
                }
            }
        }

        private Task<T> PerformReadAsync<TArg, T>(Func<TArg, T> func, TArg arg, CancellationToken cancellationToken)
            where TArg : struct
        {
            // Use a pooled strongbox so we don't box the provided arg.
            var box = StrongBoxes<TArg>.GetBox();
            box.Value = arg;

            return Task.Factory.StartNew<T>(b =>
            {
                var innerBox = (StrongBox<TArg>)b;
                var result = func(innerBox.Value);
                StrongBoxes<TArg>.ReturnBox(innerBox);
                return result;
            }, box, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ConcurrentScheduler);
        }

        public Task<bool> PerformWriteAsync<TArg>(Func<TArg, bool> func, TArg arg, CancellationToken cancellationToken)
            where TArg : struct
        {
            // Use a pooled strongbox so we don't box the provided arg.
            var box = StrongBoxes<TArg>.GetBox();
            box.Value = arg;

            return Task.Factory.StartNew(b =>
            {
                var innerBox = (StrongBox<TArg>)b;
                var result = func(innerBox.Value);
                StrongBoxes<TArg>.ReturnBox(innerBox);
                return result;
            }, box, cancellationToken, TaskCreationOptions.None, _readerWriterLock.ExclusiveScheduler);
        }
    }
}
