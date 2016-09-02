// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static partial class Extensions
    {
        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            try
            {
                using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (var stream = new ServerDirectStream())
                {
                    // send request by adding direct stream name to end of arguments
                    var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    await funcWithDirectStreamAsync(stream, mergedCancellation.Token).ConfigureAwait(false);

                    // wait task to finish
                    await task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // if cancelled due to us, throw cancellation exception.
                cancellationToken.ThrowIfCancellationRequested();

                // if canclled due to invocation is failed, rethrow merged cancellation
                throw;
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            try
            {
                using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (var stream = new ServerDirectStream())
                {
                    // send request to asset source
                    var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    var result = await funcWithDirectStreamAsync(stream, mergedCancellation.Token).ConfigureAwait(false);

                    // wait task to finish
                    await task.ConfigureAwait(false);

                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                // if cancelled due to us, throw cancellation exception.
                cancellationToken.ThrowIfCancellationRequested();

                // if canclled due to invocation is failed, rethrow merged cancellation
                throw;
            }
        }

        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Action<Stream, CancellationToken> actionWithDirectStream, CancellationToken cancellationToken)
        {
            try
            {
                using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (var stream = new ServerDirectStream())
                {
                    // send request by adding direct stream name to end of arguments
                    var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    actionWithDirectStream(stream, mergedCancellation.Token);

                    // wait task to finish
                    await task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // if cancelled due to us, throw cancellation exception.
                cancellationToken.ThrowIfCancellationRequested();

                // if canclled due to invocation is failed, rethrow merged cancellation
                throw;
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, T> funcWithDirectStream, CancellationToken cancellationToken)
        {
            try
            {
                using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (var stream = new ServerDirectStream())
                {
                    // send request to asset source
                    var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    var result = funcWithDirectStream(stream, mergedCancellation.Token);

                    // wait task to finish
                    await task.ConfigureAwait(false);

                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                // if cancelled due to us, throw cancellation exception.
                cancellationToken.ThrowIfCancellationRequested();

                // if canclled due to invocation is failed, rethrow merged cancellation
                throw;
            }
        }

        private static void RaiseCancellationIfInvokeFailed(Task task, CancellationTokenSource mergedCancellation, CancellationToken cancellationToken)
        {
            // if invoke throws an exception, make sure we raise cancellation
            var dummy = task.ContinueWith(_ =>
            {
                try
                {
                    mergedCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // merged cancellation is already disposed
                }
            }, cancellationToken, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
