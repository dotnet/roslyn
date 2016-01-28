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
        /// <summary>
        /// Wait for all of the <see cref="IAsynchronousOperationWaiter"/> instances to finish their
        /// work.
        /// </summary>
        /// <remarks>
        /// This is a very handy method for debugging hangs in the unit test.  Set a break point in the 
        /// loop, dig into the waiters and see all of the active <see cref="IAsyncToken"/> values 
        /// representing the remaining work.
        /// </remarks>
        internal static async Task WaitAllAsync(this IEnumerable<IAsynchronousOperationWaiter> waiters)
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
