﻿// Licensed to the .NET Foundation under one or more agreements.
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
            => PerformTaskAsync(func, arg, _connectionPoolService.Scheduler.ConcurrentScheduler, cancellationToken);

        // Write tasks go to the exclusive-scheduler so they run exclusively of all other threading
        // tasks we need to do.
        public Task<bool> PerformWriteAsync<TArg>(Func<TArg, bool> func, TArg arg, CancellationToken cancellationToken) where TArg : struct
            => PerformTaskAsync(func, arg, _connectionPoolService.Scheduler.ExclusiveScheduler, cancellationToken);

        public Task PerformWriteAsync(Action action, CancellationToken cancellationToken)
            => PerformWriteAsync(vt =>
            {
                vt.Item1();
                return true;
            }, ValueTuple.Create(action), cancellationToken);
    }
}
