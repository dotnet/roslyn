// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

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
        private static class Threading<TArg, TResult>
            where TArg : struct
        {
            private static readonly Stack<StrongBox<(Func<TArg, TResult> func, TArg arg)>> _boxes =
                 new Stack<StrongBox<(Func<TArg, TResult> func, TArg arg)>>();

            private static StrongBox<(Func<TArg, TResult> func, TArg arg)> GetBox()
            {
                lock (_boxes)
                {
                    if (_boxes.Count > 0)
                    {
                        return _boxes.Pop();
                    }

                    return new StrongBox<(Func<TArg, TResult> func, TArg arg)>();
                }
            }

            private static void ReturnBox(StrongBox<(Func<TArg, TResult> func, TArg arg)> box)
            {
                lock (_boxes)
                {
                    _boxes.Push(box);
                }
            }

            private static readonly Func<object, TResult> Callback = b =>
            {
                var innerBox = (StrongBox<(Func<TArg, TResult> func, TArg arg)>)b;
                var (func, arg) = innerBox.Value;
                ReturnBox(innerBox);
                return func(arg);
            };

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            public static Task<TResult> PerformTask(Func<TArg, TResult> func, TArg arg, TaskScheduler scheduler, CancellationToken cancellationToken)
            {
                // Use a pooled strongbox so we don't box the provided arg.
                var box = GetBox();
                box.Value = (func, arg);

                return Task.Factory.StartNew(Callback, box, cancellationToken, TaskCreationOptions.None, scheduler);
            }
        }

        // Read tasks go to the concurrent-scheduler where they can run concurrently with other read
        // tasks.
        private Task<TResult> PerformReadAsync<TArg, TResult>(Func<TArg, TResult> func, TArg arg, CancellationToken cancellationToken) where TArg : struct
            => Threading<TArg, TResult>.PerformTask(func, arg, _readerWriterLock.ConcurrentScheduler, cancellationToken);

        // Write tasks go to the exclusive-scheduler so they run exclusively of all other threading
        // tasks we need to do.
        public Task<bool> PerformWriteAsync<TArg>(Func<TArg, bool> func, TArg arg, CancellationToken cancellationToken) where TArg : struct
            => Threading<TArg, bool>.PerformTask(func, arg, _readerWriterLock.ExclusiveScheduler, cancellationToken);
    }
}
