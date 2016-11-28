// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static partial class Extensions
    {
        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Action<Stream, CancellationToken> actionWithDirectStream, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments,
            Func<Stream, CancellationToken, T> funcWithDirectStream, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        private static bool ReportUnlessCanceled(Exception ex, CancellationToken token)
        {
            // check whether we are in cancellation mode
            if (token.IsCancellationRequested)
            {
                // we are under cancellation, we don't care what the exception is.
                // due to the way we do cancellation (forcefully closing connection in the middle of reading/writing)
                // various exceptions can be thrown. for example, if we close our own named pipe stream in the middle of
                // object reader/writer using it, we could get invalid operation exception or invalid cast exception.
                return true;
            }

            // unexpected exception case. crash VS
            return FatalError.Report(ex);
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
