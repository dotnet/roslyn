﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal static class Extensions
    {
        public static Task WaitOneAsync(this WaitHandle handle, int? timeoutMilliseconds = null)
        {
            RegisteredWaitHandle registeredHandle = null;
            var tcs = new TaskCompletionSource<object>();
            registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                handle,
                (_, timeout) =>
                {
                    tcs.TrySetResult(null);
                    if (registeredHandle is object)
                    {
                        registeredHandle.Unregister(waitObject: null);
                    }
                },
                null,
                timeoutMilliseconds ?? -1,
                executeOnlyOnce: true);
            return tcs.Task;
        }

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
