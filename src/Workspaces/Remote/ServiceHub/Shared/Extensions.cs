﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            this JsonRpc rpc, string targetName, IReadOnlyList<object> arguments,
            Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
                {
                    // send request by adding direct stream name to end of arguments
                    var task = rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), mergedCancellation.Token);

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    await funcWithDirectStreamAsync(stream, mergedCancellation.Token).ConfigureAwait(false);

                    // wait task to finish
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    // but we need merged one to cancel operation if InvokeAsync has failed. if it failed without
                    // cancellation token is raised, then we do want to have watson report
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IReadOnlyList<object> arguments,
            Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
                {
                    // send request to asset source
                    var task = rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), mergedCancellation.Token);

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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IReadOnlyList<object> arguments,
            Action<Stream, CancellationToken> actionWithDirectStream, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
                {
                    // send request by adding direct stream name to end of arguments
                    var task = rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), mergedCancellation.Token);

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    actionWithDirectStream(stream, mergedCancellation.Token);

                    // wait task to finish
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IReadOnlyList<object> arguments,
            Func<Stream, CancellationToken, T> funcWithDirectStream, CancellationToken cancellationToken)
        {
            using (var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stream = new ServerDirectStream())
            {
                try
                {
                    // send request to asset source
                    var task = rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), mergedCancellation.Token);

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
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }

#pragma warning disable CA1068 // this method accepts 2 cancellation tokens
        private static bool ReportUnlessCanceled(Exception ex, CancellationToken remoteToken, CancellationToken hostToken)
#pragma warning restore CA1068 // CancellationToken parameters must come last
        {
            // check whether we are in cancellation mode

            // things are either cancelled by us (hostToken) or cancelled by OOP (remoteToken). 
            // "cancelled by us" means operation user invoked is cancelled by another user action such as explicit cancel, or typing.
            // "cancelled by OOP" means operation user invoked is cancelled due to issue on OOP such as user killed OOP process.

            if (hostToken.IsCancellationRequested)
            {
                // we are under our own cancellation, we don't care what the exception is.
                // due to the way we do cancellation (forcefully closing connection in the middle of reading/writing)
                // various exceptions can be thrown. for example, if we close our own named pipe stream in the middle of
                // object reader/writer using it, we could get invalid operation exception or invalid cast exception.
                return true;
            }

            if (remoteToken.IsCancellationRequested)
            {
                // now we allow connection to be closed by users by killing remote host process.
                // in those case, it will be converted to remote token cancellation. we accept that as known
                // exception, and allow us to not crash
                return true;
            }

            // unexpected exception case. crash VS
            return FatalError.Report(ex);
        }

        private static void RaiseCancellationIfInvokeFailed(Task task, CancellationTokenSource mergedCancellation, CancellationToken cancellationToken)
        {
            // if invoke throws an exception, make sure we raise cancellation
            var dummy = task.ContinueWith(p =>
            {
                try
                {
                    // now, we allow user to kill OOP process, when that happen, 
                    // just raise cancellation. 
                    // otherwise, stream.WaitForDirectConnectionAsync can stuck there forever since
                    // cancellation from user won't be raised
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
