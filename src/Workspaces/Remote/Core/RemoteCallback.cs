// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        /// Invokes a remote API that streams results back to the caller.
        /// </summary>
        /// <inheritdoc cref="BrokeredServiceConnection{TService}.InvokeStreamingServiceAsync"/>
        public async ValueTask<TResult> InvokeAsync<TResult>(
            Func<T, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            try
            {
                return await BrokeredServiceConnection<T>.InvokeStreamingServiceAsync(_callback, invocation, reader, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(exception, cancellationToken);
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

                return true;
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
                throw new OperationCanceledNotMatchingCancellationTokenException(exception);
            }

            // If this is hit the cancellation token passed to the service implementation did not use the correct token,
            // and the resulting exception was not a ConnectionLostException.
            return ExceptionUtilities.Unreachable;
        }
    }
}
