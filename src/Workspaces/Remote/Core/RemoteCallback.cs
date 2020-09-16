// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Wraps calls from a remote brokered service back to the client or to an in-proc brokered service.
    /// The purpose of this type is to handle exceptions thrown by the underlying remoting infrastructure
    /// in manner that's compatible with our exception handling policies.
    /// 
    /// TODO: This wrapper might not be needed once https://github.com/microsoft/vs-streamjsonrpc/issues/246 is fixed.
    /// </summary>
    internal readonly struct RemoteCallback<T>
        where T : class
    {
        private readonly T _callback;

        public readonly CancellationTokenSource ClientDisconnectedSource;

        public RemoteCallback(T callback, CancellationTokenSource clientDisconnectedSource)
        {
            _callback = callback;
            ClientDisconnectedSource = clientDisconnectedSource;
        }

        public async ValueTask InvokeAsync(Func<T, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                await invocation(_callback, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(cancellationToken);
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
                throw OnUnexpectedException(cancellationToken);
            }
        }

        /// <summary>
        /// Invokes a remote API that streams results back to the caller.
        /// </summary>
        public async ValueTask<TResult> InvokeAsync<TResult>(
            Func<T, Stream, CancellationToken, ValueTask> invocation,
            Func<Stream, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            try
            {
                return await BrokeredServiceConnection<T>.InvokeStreamingServiceAsync(_callback, invocation, reader, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw OnUnexpectedException(cancellationToken);
            }
        }

        // TODO: https://github.com/microsoft/vs-streamjsonrpc/issues/246
        //
        // We need to get to a state when remote calls can only throw 4 types of exceptions that correspond to
        //   1) Connection issue (connection dropped for any reason)
        //   2) Serialization issue - bug in serialization of arguments (types are not serializable, etc.)
        //   3) Remote exception - an exception was thrown by the callee
        //   4) Cancelation
        // When a connection is dropped and CancelLocallyInvokedMethodsWhenConnectionIsClosed is set the connection dropped exception [1] should not be thrown.
        // Instead a the cancellation token should be signaled and OperationCancelledException should be thrown ([4]).
        //
        // Until the above issue in JSON-RPC is fixed we do a best guess on what the issue is.

        private bool ReportUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is RemoteInvocationException or JsonException)
            {
                // indicates bug on client side or in serialization, propagate the exception
                return FatalError.ReportWithoutCrashAndPropagate(exception);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // If cancelation is requested and we see a different exception the handler will throw OperationCancelledException.
                return exception is not OperationCanceledException;
            }

            // We assume that any other exception indicates lost connection (it might not),
            // cancel any ongoing work since the client can't receive the results.
            // This should be handled by JSON-RPC but it's not guaranteed due to https://github.com/microsoft/vs-streamjsonrpc/issues/246.
            ClientDisconnectedSource.Cancel();

            // catch the exception, cancellation exception will be thrown by the handler.
            return true;
        }

        private static Exception OnUnexpectedException(CancellationToken cancellationToken)
        {
            // Remote call may fail with different exception even when our cancellation token is signaled
            // (e.g. on shutdown if the connection is dropped):
            cancellationToken.ThrowIfCancellationRequested();

            // If this is hit the cancellation token passed to the service implementation did not use the correct token.
            return ExceptionUtilities.Unreachable;
        }
    }
}
