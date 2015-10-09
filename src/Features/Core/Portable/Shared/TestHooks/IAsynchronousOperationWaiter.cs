// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IAsynchronousOperationWaiter
    {
        bool TrackActiveTokens { get; set; }
        ImmutableArray<AsynchronousOperationListener.DiagnosticAsyncToken> ActiveDiagnosticTokens { get; }

        Task CreateWaitTask();
        bool HasPendingWork { get; }
    }

    internal static class AsynchronousOperationWaiter
    {
        internal static async Task PumpingWaitAllAsync(this IEnumerable<IAsynchronousOperationWaiter> waiters)
        {
            var smallTimeout = TimeSpan.FromMilliseconds(10);
            var tasks = waiters.Select(x => x.CreateWaitTask()).ToList();
            var done = false;
            while (!done)
            {
                done = await tasks.WhenAll(smallTimeout).ConfigureAwait(true);
            }

            foreach (var task in tasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }
        }

        private static async Task<bool> WhenAll(this IEnumerable<Task> tasks, TimeSpan timeout)
        {
            var delay = Task.Delay(timeout);
            var list = tasks.Where(x => !x.IsCompleted).ToList();

            list.Add(delay);
            do
            {
                await Task.WhenAny(list).ConfigureAwait(true);
                list.RemoveAll(x => x.IsCompleted);
                if (list.Count == 0)
                {
                    return true;
                }

                if (delay.IsCompleted)
                {
                    return false;
                }
            } while (true);
        }

    }
}
