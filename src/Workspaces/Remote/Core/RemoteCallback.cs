// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Wraps calls from a remote brokered service back to the client or to an in-proc brokered service.
    /// The purpose of this type is to handle exceptions thrown by the underlying remoting infrastructure
    /// in manner that's compatible with our exception handling policies.
    /// </summary>
    internal readonly struct RemoteCallback<T>
        where T : class
    {
        private readonly T _callback;

        public RemoteCallback(T callback)
        {
            _callback = callback;
        }

        /// <summary>
        /// Use to perform a callback from ServiceHub process to an arbitrary brokered service hosted in the original process (usually devenv).
        /// </summary>
        public static async ValueTask<TResult> InvokeServiceAsync<TResult>(
            ServiceBrokerClient client,
            ServiceRpcDescriptor serviceDescriptor,
            Func<RemoteCallback<T>, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken)
        {
            ServiceBrokerClient.Rental<T> rental;
            try
            {
                rental = await client.GetProxyAsync<T>(serviceDescriptor, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException e)
            {
                // When a connection is dropped ServiceHub's ServiceManager disposes the brokered service, which in turn disposes the ServiceBrokerClient.
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledIgnoringCallerTokenException(e);
            }

            Contract.ThrowIfNull(rental.Proxy);
            var callback = new RemoteCallback<T>(rental.Proxy);

            return await invocation(callback, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the currently executing brokered service hosted in ServiceHub process.
        /// </summary>
        public async ValueTask InvokeAsync(Func<T, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                await invocation(_callback, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(exception, cancellationToken);
            }
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the currently executing brokered service hosted in ServiceHub process.
        /// </summary>
        public async ValueTask<TResult> InvokeAsync<TResult>(Func<T, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                return await invocation(_callback, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(exception, cancellationToken);
            }
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the
        /// currently executing brokered service hosted in ServiceHub process. The API streams results back to the
        /// caller.
        /// </summary>
        /// <param name="invocation">A callback to asynchronously write data. The callback should always <see
        /// cref="PipeWriter.Complete"/> the <see cref="PipeWriter"/>.  If it does not then reading will hang</param>
        /// <param name="reader">A callback to asynchronously read data. The callback should not complete the <see
        /// cref="PipeReader"/>, but no harm will happen if it does.</param>
        /// <param name="cancellationToken">A cancellation token the operation will observe.</param>
        public async ValueTask<TResult> InvokeAsync<TResult>(
            Func<T, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            try
            {
                return await InvokeWorkerAsync(_callback).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(exception, cancellationToken);
            }

            async ValueTask<TResult> InvokeWorkerAsync(T service)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pipe = new Pipe();

                // Kick off the work to do the writing to the pipe in a fire-and-forget fashion.  It will start hot and
                // will be able to do work as the reading side attempts to pull in the data it is writing.

                _ = WriteAsync(service, pipe.Writer);

                try
                {
                    return await reader(pipe.Reader, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // ensure we always complete the reader so the pipe can clean up all its resources. in the case of
                    // an exception, attempt to complete the reader with that as well as that will tear down the writer
                    // allowing it to stop writing and allowing the pipe to be cleaned up.
                    await pipe.Reader.CompleteAsync(e).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    // ensure we always complete the reader so the pipe can clean up all its resources.
                    await pipe.Reader.CompleteAsync().ConfigureAwait(false);
                }
            }

            async Task WriteAsync(T service, PipeWriter writer)
            {
                try
                {
                    // Intentionally yield this thread so that the caller can proceed in parallel.
                    await TaskScheduler.Default;

                    await invocation(service, writer, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Ensure that the writer is complete if an exception is thrown. This intentionally swallows the
                    // exception on this side, knowing it will actually be thrown on the reading side.
                    await writer.CompleteAsync(e).ConfigureAwait(false);
                }
#if false
                // Absolutely do not Complete/CompleteAsync the writer here.  The writer is passed to StreamJsonRPC
                // which takes ownership of it.  The *inside* of that rpc is responsible for Completing the writer *it*
                // is passed, which will signal the completion of the writer we have here.  Note: we *do* need to
                // complete this writer in the event if an exception as that may have happened *prior* to even issuing
                // the rpc.  If we don't complete the writer we will hang.  If the exception happened within the RPC
                // the writer may already be completed, but it's fine for us to complete it a second time.
                //
                // It is *not* fine for us to complete the writer in a non-exception event as it is legal (and is the
                // case) that the code in StreamJsonRPC may still be using it (see
                // https://github.com/AArnott/Nerdbank.Streams/blob/dafeb5846702bc29e261c9ddf60f42feae01654c/src/Nerdbank.Streams/PipeExtensions.cs#L428)
                // where the writer may be advanced in an independent Task even once the rpc message has returned to the
                // caller (us). 
                finally
                {
                }
#endif
            }
        }

        // Remote calls can only throw 4 types of exceptions that correspond to
        //
        //   1) Connection issue (connection dropped for any reason)
        //   2) Serialization issue - bug in serialization of arguments (types are not serializable, etc.)
        //   3) Remote exception - an exception was thrown by the callee
        //   4) Cancelation
        //
        private static bool ReportUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is IOException)
            {
                // propagate intermittent exceptions without reporting telemetry:
                return false;
            }

            if (exception is OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Cancellation was requested and expected
                    return false;
                }

                // Log unexpected state where a cancellation exception occurs without being requested.
                return FatalError.ReportAndCatch(exception);
            }

            // When a connection is dropped we can see ConnectionLostException even though CancelLocallyInvokedMethodsWhenConnectionIsClosed is set.
            // That's because there might be a delay between the JsonRpc detecting the disconnect and the call attempting to send a message.
            // Catch the ConnectionLostException exception here and convert it to OperationCanceledException.
            if (exception is ConnectionLostException)
            {
                return true;
            }

            // Indicates bug on client side or in serialization, report NFW and propagate the exception.
            return FatalError.ReportAndPropagate(exception);
        }

        private static Exception OnUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is ConnectionLostException)
            {
                throw new OperationCanceledIgnoringCallerTokenException(exception);
            }

            // If this is hit the cancellation token passed to the service implementation did not use the correct token,
            // and the resulting exception was not a ConnectionLostException.
            return ExceptionUtilities.Unreachable();
        }
    }
}
