// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Nerdbank.Streams;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class BrokeredServiceConnection<TService> : RemoteServiceConnection<TService>
        where TService : class
    {
        private readonly IErrorReportingService? _errorReportingService;
        private readonly IRemoteHostClientShutdownCancellationService? _shutdownCancellationService;
        private readonly SolutionAssetStorage _solutionAssetStorage;
        private readonly TService _service;

        public BrokeredServiceConnection(
            TService service,
            SolutionAssetStorage solutionAssetStorage,
            IErrorReportingService? errorReportingService,
            IRemoteHostClientShutdownCancellationService? shutdownCancellationService)
        {
            _errorReportingService = errorReportingService;
            _shutdownCancellationService = shutdownCancellationService;
            _solutionAssetStorage = solutionAssetStorage;
            _service = service;
        }

        public override void Dispose()
            => (_service as IDisposable)?.Dispose();

        // without solution

        public override async ValueTask<bool> TryInvokeAsync(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                await invocation(_service, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                return await invocation(_service, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Func<TService, Stream, CancellationToken, ValueTask> invocation,
            Func<Stream, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            try
            {
                return await InvokeStreamingServiceAsync(_service, invocation, reader, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // with solution

        public override async ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                await invocation(_service, scope.SolutionInfo, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                return await invocation(_service, scope.SolutionInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
            Solution solution,
            Func<TService, PinnedSolutionInfo, Stream, CancellationToken, ValueTask> invocation,
            Func<Stream, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                return await InvokeStreamingServiceAsync(
                    _service,
                    (service, stream, cancellationToken) => invocation(service, scope.SolutionInfo, stream, cancellationToken),
                    reader,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        internal static async ValueTask<TResult> InvokeStreamingServiceAsync<TResult>(
            TService service,
            Func<TService, Stream, CancellationToken, ValueTask> invocation,
            Func<Stream, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            // The reader should close the client stream, the writer will close the server stream.
            // See https://github.com/microsoft/vs-streamjsonrpc/blob/master/doc/oob_streams.md
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            // Create new tasks that both start executing, rather than invoking the delegates directly.
            // If the reader started synchronously reading before the writer task started it would deadlock, and vice versa
            // if the writer synchronously filled the buffer before the reader task started it would also deadlock.
            var writerTask = Task.Run(async () => await invocation(service, serverStream, cancellationToken).ConfigureAwait(false), cancellationToken);
            var readerTask = Task.Run(async () => await reader(clientStream, cancellationToken).ConfigureAwait(false), cancellationToken);
            await Task.WhenAll(writerTask, readerTask).ConfigureAwait(false);

            return readerTask.Result;
        }

        private bool ReportUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            // Do not report telemetry when the host is shutting down or the remote service threw an IO exception:
            if (IsHostShuttingDown || IsRemoteIOException(exception))
            {
                return true;
            }

            // report telemetry event:
            Logger.Log(FunctionId.FeatureNotAvailable, $"{ServiceDescriptors.GetServiceName(typeof(TService))}: {exception.GetType()}: {exception.Message}");

            return FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken);
        }

        private bool IsHostShuttingDown
            => _shutdownCancellationService?.ShutdownToken.IsCancellationRequested == true;

        // TODO: we need https://github.com/microsoft/vs-streamjsonrpc/issues/468 to be implemented in order to check for IOException subtypes.
        private static bool IsRemoteIOException(Exception exception)
            => exception is RemoteInvocationException { ErrorData: CommonErrorData { TypeName: "System.IO.IOException" } };

        private void OnUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            // If the cancellation token passed to the remote call is not linked with the host shutdown cancellation token,
            // various non-cancellation exceptions may occur during the remote call.
            // Throw cancellation exception if the cancellation token is signaled.
            // If it is not then show info to the user that the service is not available dure to shutdown.
            cancellationToken.ThrowIfCancellationRequested();

            if (_errorReportingService == null)
            {
                return;
            }

            // Show the error on the client. See https://github.com/dotnet/roslyn/issues/40476 for error classification details.
            // Based on the exception type and the state of the system we report one of the following:
            // - "Feature xyz is currently unavailable due to an intermittent error. Please try again later. Error message: '{1}'" (RemoteInvocationException: IOException)
            // - "Feature xyz is currently unavailable due to an internal error [Details]" (exception is RemoteInvocationException, MessagePackSerializationException, ConnectionLostException)
            // - "Feature xyz is currently unavailable since Visual Studio is shutting down" (connection exceptions during shutdown cancellation when cancellationToken is not signalled)

            // We expect all RPC calls to complete and not drop the connection.
            // ConnectionLostException indicates a bug that is likely thrown because the remote process crashed.
            // Currently, ConnectionLostException is also throw when the result of the RPC method fails to serialize 
            // (see https://github.com/microsoft/vs-streamjsonrpc/issues/549)

            string message;
            Exception? internalException = null;
            var featureName = ServiceDescriptors.GetFeatureName(typeof(TService));

            if (IsRemoteIOException(exception))
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_due_to_an_intermittent_error, featureName, exception.Message);
            }
            else if (IsHostShuttingDown)
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_host_shutting_down, featureName, _errorReportingService.HostDisplayName);
            }
            else
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_due_to_an_internal_error, featureName);
                internalException = exception;
            }

            _errorReportingService.ShowFeatureNotAvailableErrorInfo(message, internalException);
        }
    }
}
