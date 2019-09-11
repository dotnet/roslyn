// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal static class Extensions
    {
        public static Task<bool> ToTask(this WaitHandle handle, int? timeoutMilliseconds)
        {
            RegisteredWaitHandle registeredHandle = null;
            var tcs = new TaskCompletionSource<bool>();
            registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                handle,
                (_, timedOut) =>
                {
                    tcs.TrySetResult(!timedOut);
                    if (!timedOut)
                    {
                        registeredHandle.Unregister(waitObject: null);
                    }
                },
                null,
                timeoutMilliseconds ?? -1,
                executeOnlyOnce: true);
            return tcs.Task;
        }

        public static async Task<bool> WaitOneAsync(this WaitHandle handle, int? timeoutMilliseconds = null) => await handle.ToTask(timeoutMilliseconds);

        public static async ValueTask<T> TakeAsync<T>(this BlockingCollection<T> collection, TimeSpan? pollTimeSpan = null, CancellationToken cancellationToken = default)
        {
            var delay = pollTimeSpan ?? TimeSpan.FromSeconds(.25);
            do
            {
                if (collection.TryTake(out T value))
                {
                    return value;
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            } while (true);
        }
    }
}
